using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;
using IocpSharp.Http.Streams;
using IocpSharp.Http.Utils;

namespace IocpSharp.Http
{
    public class HttpRequest : IDisposable
    {

        private string _method = null;
        private string _url = null;
        private string _httpProtocol = null;
        private string _originHeaders = "";
        private NameValueCollection _headers = new NameValueCollection();
        private Stream _baseStream = null;

        public string Method => _method;
        public string Url => _url;
        public string HttpProtocol => _httpProtocol;
        public string OriginHeaders => _originHeaders;
        public NameValueCollection Headers => _headers;


        internal HttpRequest(Stream baseStream) {
            _baseStream = baseStream;
        }
        public HttpRequest(string url, string method = "GET", string httpProtocol = "HTTP/1.1")
        {
            _url = url;
            _method = method;
            _httpProtocol = httpProtocol;
        }

        public string GetAllRequestHeaders()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}\r\n", _method, _url, _httpProtocol);

            foreach (string name in _headers.Keys)
            {
                string[] values = _headers.GetValues(name);
                if (values == null || values.Length == 0) continue;

                foreach (string value in values)
                {
                    if (value == null) continue;
                    sb.AppendFormat("{0}: {1}\r\n", name, value);
                }
            }
            sb.Append("\r\n");

            return sb.ToString();
        }

        /// <summary>
        /// 这里只是简单的给源请求数据补了新行
        /// 可以在Ready方法里面做更多事情
        /// 例如解析Host、ContentLength、ContentType、AcceptEncoding、Connection以及Range等请求头
        /// </summary>
        protected virtual HttpRequest Ready()
        {
            _originHeaders += "\r\n";

            int idx = _url.IndexOf('?');
            _path = _url;
            if (idx >= 0)
            {
                _path = _url.Substring(0, idx);
                _query = _url.Substring(idx).TrimStart('?');
            }

            //解析Content-Length
            string header = _headers["content-length"];
            if (!string.IsNullOrEmpty(header))
            {
                if (!long.TryParse(header, out _contentLength))
                {
                    throw new HttpRequestException(HttpRequestError.ContentLengthError);
                }
            }

            //获取Transfer-Encoding
            _transferEncoding = _headers["transfer-encoding"];

            //解析Content-Type
            header = _headers["content-type"];
            if (!string.IsNullOrEmpty(header))
            {
                _contentType = HttpHeaderProperty.Parse(header);
                ParseRequestContent();
            }

            return this;
        }

        private HttpHeaderProperty _contentType = null;
        private long _contentLength = -1;
        private string _transferEncoding = null;

        //multipart/form-data为文件上传
        public bool IsUpload => _contentType != null && _contentType.Value == "multipart/form-data" && !string.IsNullOrEmpty(_contentType["boundary"]);
        public string ContentType => _contentType == null ? null : _contentType.Value;
        public string Boundary => _contentType == null ? null : _contentType["boundary"];

        private List<FileItem> _files = null;

        public List<FileItem> Files
        {
            get
            {
                if (!IsUpload) return null;
                if(_files == null) ReadUploadContent();
                return _files;
            }
        }

        private void ParseRequestContent()
        {
            //有文件上传，自动解析
            if (IsUpload)
            {
                if (!HttpServerBase.AutoParseUpload) return;
                ReadUploadContent();
                return;
            }

            if (HasEntityBody) ReadRequestBody();
            else _requestBody = new byte[0];

        }

        private void ReadUploadContent()
        {
            var parser = new HttpMultipartFormDataParser(HttpServerBase.UplaodTempDir);
            using Stream input = OpenRead();
            parser.Parse(input, Boundary);
            _form = parser.Forms;
            _files = parser.Files;
        }

        private void ReadRequestBody()
        {

            using MemoryStream output = new MemoryStream();
            using (Stream input = OpenRead())
            {
                input.CopyTo(output);
            }
            _requestBody = output.ToArray();
        }

        #region QueryString相关参数和属性

        private string _path = null;
        private string _query = null;
        private NameValueCollection _queryString = null;

        public string Path => _path;
        public string Query => _query;
        public NameValueCollection QueryString
        {
            get
            {
                //在获取属性值时才初始化_queryString的值
                if (_queryString != null) return _queryString;

                return _queryString = HttpUtility.ParseUriComponents(_query);
            }
        }

        #endregion

        #region 请求实体相关参数和属性
        private byte[] _requestBody = null;
        private NameValueCollection _form = null;


        /// <summary>
        /// 从请求中读取请求实体
        /// 需要把数据缓存起来
        /// </summary>
        public byte[] RequestBody
        {
            get
            {
                if (_requestBody != null) return _requestBody;
                if (!HasEntityBody) return _requestBody = new byte[0];
                ReadRequestBody();
                return _requestBody;
            }
        }
        public NameValueCollection Form
        {
            get
            {
                //在获取属性值时才初始化_form的值
                if (_form != null) return _form;

                if(IsUpload && _files == null)
                {
                    ReadUploadContent();
                    return _form;
                }

                return _form = HttpUtility.ParseUriComponents(Encoding.UTF8.GetString(RequestBody));
            }
        }

        #endregion

        /// <summary>
        /// 确认请求是否包含消息
        /// </summary>
        public bool HasEntityBody => _contentLength > 0
            || !string.IsNullOrEmpty(_transferEncoding);


        private Stream _entityReadStream = null;
        public Stream OpenRead()
        {
            if (!HasEntityBody) throw new Exception("请求不包含消息");

            if(_entityReadStream != null)
            {
                return _entityReadStream;
            }

            //如果同时出现transfer-encoding和content-length
            //优先处理transfer-encoding，忽略content-length
            if (!string.IsNullOrEmpty(_transferEncoding))
            {
                //返回一个ChunkedReadStream
                return _entityReadStream = new ChunkedReadStream(_baseStream, true);
            }

            //返回一个ContentedReadStream
            return _entityReadStream = new ContentedReadStream(_contentLength, _baseStream, true);
        }

        /// <summary>
        /// 关于请求实体，RFC2616有一句‘A server SHOULD read and forward a message-body on any request;’
        /// 对于任何request，服务端应该将请求实体“读完”
        /// 我们必须得这么做，也就是将OpenRead打开的流读完
        /// 否则在KeepAlive保持的长链里，极有可能造成“脏读”，导致下一个request没法被正常解析
        /// </summary>
        public void EnsureEntityBodyRead()
        {
            //没有请求实体或者数据已经被读了，忽略
            if (!HasEntityBody || _entityReadStream != null) return;
            using(Stream forward = OpenRead())
            {
                byte[] forwardBytes = new byte[32768];
                //读取，丢弃
                while (forward.Read(forwardBytes, 0, 32768) > 0) ;
            }
        }


        private bool _firstLineParsed = false;
        /// <summary>
        /// 解析请求头首行，例如：GET / HTTP/1.1
        /// </summary>
        /// <param name="line"></param>
        private void ParseFirstLine(string line)
        {
            //判断第一个空格，用于截取请求方法
            int idx = line.IndexOf(' ');
            if (idx <= 0)
                throw new HttpRequestException(HttpRequestError.NotWellFormed);

            //判断最后一个空格，用于截取协议
            int idxEnd = line.LastIndexOf(' ');
            if (idxEnd <= 0
                || idxEnd == line.Length - 1
                || idx == idxEnd)
                throw new HttpRequestException(HttpRequestError.NotWellFormed);

            //截取请求方法，url和协议
            _method = line.Substring(0, idx);
            _httpProtocol = line.Substring(idxEnd + 1);
            _url = line.Substring(idx + 1, idxEnd - idx - 1).Trim();

            if (string.IsNullOrEmpty(_url))
                throw new HttpRequestException(HttpRequestError.NoneUrl);

        }

        /// <summary>
        /// 解析请求头的每一行
        /// </summary>
        /// <param name="line">行</param>
        private void ParseLine(string line)
        {
            _originHeaders += line + "\r\n";

            //首行包含请求方法，url和协议等。
            if (!_firstLineParsed)
            {
                ParseFirstLine(line);
                _firstLineParsed = true;
                return;
            }

            //解析后续数据，行格式(Key: Value)
            //冒号分割的请求行
            int rowIdx = line.IndexOf(':');
            if (rowIdx <= 0 || rowIdx == line.Length - 1)
                throw new HttpRequestException(HttpRequestError.NotWellFormed);


            _headers.Add(line.Substring(0, rowIdx).Trim(), line.Substring(rowIdx + 1).Trim());
        }


        /// <summary>
        /// 从一个网络流中抓取一个HttpRequest
        /// </summary>
        /// <param name="source">任何支持读取的数据流，包括网络流</param>
        /// <returns>包含请求信息的HttpRequest</returns>
        public static HttpRequest Capture(Stream source)
        {
            byte[] lineBuffer = new byte[65536];

            HttpRequest request = new HttpRequest(source);
            try
            {
                //循环读取请求头，解析每一行
                while (true)
                {
                    string line = ReadLine(source, lineBuffer);

                    //遇到空行，说明请求头读取完毕，返回
                    if (string.IsNullOrEmpty(line))
                        return request.Ready();

                    //在HttpRequest实例中，解析每一行的数据
                    request.ParseLine(line);
                }
            }
            catch
            {
                request.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 从流中读取一行数据，协议要求，行必须以\r\n结尾
        /// 因为是演示，这里直接同步方式，按字节读
        /// 通常，为了提高效率，程序都是预先读取一大块数据，从读取的数据中检索请求头
        /// 而不是频繁调用Socket的Receive方法（NetworkStream的内部实现）
        /// </summary>
        /// <param name="source">数据流</param>
        /// <param name="lineBuffer">缓冲区</param>
        /// <returns>数据行</returns>
        private static string ReadLine(Stream source, byte[] lineBuffer)
        {

            int offset = 0;
            int chr;

            while ((chr = source.ReadByte()) > 0)
            {
                lineBuffer[offset] = (byte)chr;
                if (chr == '\n')
                {
                    //协议要求，每行必须以\r\n结束
                    if (offset < 1 || lineBuffer[offset - 1] != '\r')
                        throw new HttpRequestException(HttpRequestError.NotWellFormed);

                    if (offset == 1)
                        return "";

                    //可以使用具体的编码来获取字符串数据，例如Encoding.UTF8
                    //这里使用ASCII读取
                    return Encoding.ASCII.GetString(lineBuffer, 0, offset - 1);
                }
                offset++;
                //请求头的每行太长，抛出异常
                if (offset >= lineBuffer.Length)
                    throw new HttpRequestException(HttpRequestError.LineLengthExceedsLimit);
            }
            //请求头还没解析完就没数据了
            throw new HttpRequestException(HttpRequestError.ConnectionLost);
        }

        ~HttpRequest()
        {
            Disposing(false);
        }

        /// <summary>
        /// 清理工作，必要的时候关闭下实体读取的流
        /// </summary>
        /// <param name="disposing"></param>
        private void Disposing(bool disposing)
        {
            _entityReadStream?.Dispose();
            _queryString?.Clear();
            _headers?.Clear();
            _form?.Clear();
            if (disposing)
            {
                _baseStream = null;
                _entityReadStream = null;
                _url = null;
                _query = null;
                _queryString = null;
                _form = null;
                _headers = null;
                _originHeaders = null;
                _requestBody = null;
                _contentType = null;
            }
        }

        /// <summary>
        /// 实现Dispose，做一些必要的清理工作
        /// </summary>
        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
    }
}
