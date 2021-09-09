using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Streams;

namespace IocpSharp.Http.Utils
{
    public class FormItem
    {
        private string _name = null;
        private string _value = null;
        public string Name => _name;
        public string Value { get => _value; internal set => _value = value; }
        internal FormItem(string name, string value)
        {
            _name = name;
            _value = value;
        }
    }
    public class FileItem : FormItem
    {
        private string _fileName = null;
        private string _contentType = null;
        private long _fileSize = 0;
        private string _tempFile = null;

        public string FileName => _fileName;
        public string ContentType => _contentType;
        public long FileSize { get => _fileSize; internal set => _fileSize = value; }
        public string TempFile { get => _tempFile; internal set => _tempFile = value; }

        internal FileItem(string name, string fileName, string contentType) : base(name, fileName)
        {
            _fileName = fileName;
            _contentType = contentType;
        }
    }
    /// <summary>
    /// http文件上传内容解析
    /// </summary>
    public class HttpMultipartFormDataParser
    {
        private string _tempFileSaveAt = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempFileSaveAt">文件缓存目录</param>
        public HttpMultipartFormDataParser(string tempFileSaveAt)
        {
            if (!Directory.Exists(tempFileSaveAt)) throw new DirectoryNotFoundException($"上传文件缓存目录'{tempFileSaveAt}'不存在");
            _tempFileSaveAt = tempFileSaveAt;
        }
        public void Parse(Stream input, string boundary)
        {

            boundary = "--" + boundary;

            using (MultipartReadStream output = new MultipartReadStream(boundary, input, false))
            {

                byte[] lineBuffer = new byte[8192];
                string line = ReadLine(output, lineBuffer);

                if (line != boundary) throw new Exception("boundary error, unformed");

                ReadContent(output, lineBuffer);
            }
        }

        private NameValueCollection _forms = new NameValueCollection();
        private List<FileItem> _files = new List<FileItem>();

        public NameValueCollection Forms => _forms;
        public List<FileItem> Files => _files;
        private void ReadContent(MultipartReadStream input, byte[] lineBuffer)
        {
            while (true)
            {

                FormItem item = ReadContentHeader(input, lineBuffer);
                if (item == null) throw new Exception("表单数据异常");
                input.BlockHeadRead = true;
                input.BlockEndingFound = false;

                if (item is FileItem fileItem)
                {
                    if (string.IsNullOrEmpty(fileItem.FileName))
                    {

                        using MemoryStream output = new MemoryStream();
                        input.CopyTo(output);
                    }
                    else
                    {
                        string tempFile = Path.Combine(_tempFileSaveAt, Guid.NewGuid().ToString("D") + ".tmp");
                        using (FileStream output = File.OpenWrite(tempFile))
                        {
                            input.CopyTo(output);
                        }
                        fileItem.TempFile = tempFile;
                        _files.Add(fileItem);
                    }
                }
                else
                {
                    using MemoryStream output = new MemoryStream();
                    input.CopyTo(output);
                    item.Value = Encoding.UTF8.GetString(output.ToArray());
                    _forms.Add(item.Name, item.Value);
                }

                input.BlockHeadRead = false;
                string line = ReadLine(input, lineBuffer);
                if (line == null || line == "--")
                {
                    break;
                }
            }
        }

        private FormItem ReadContentHeader(Stream input, byte[] lineBuffer)
        {
            string line;
            HttpHeaderProperty contentDisposition = null;
            string contentType = null;
            while ((line = ReadLine(input, lineBuffer)) != "")
            {
                if (line == null) throw new Exception("连接断开，读取失败");

                int idx = line.IndexOf(':');
                if (idx <= 0) throw new Exception("标头错误");

                string name = line.Substring(0, idx);
                string value = line.Substring(idx + 1);
                if (string.IsNullOrEmpty(value)) continue;
                if (name == "Content-Disposition")
                {
                    contentDisposition = HttpHeaderProperty.Parse(value);
                    continue;
                }
                if (name == "Content-Type") contentType = value;
            }

            if (contentDisposition == null) return null;
            string formName = contentDisposition["name"];
            string fileName = contentDisposition["filename"];
            if (fileName == null)
            {
                return new FormItem(formName, null);
            }
            else
            {
                return new FileItem(formName, fileName, contentType);
            }
        }
        private string ReadLine(Stream stream, byte[] lineBuffer)
        {
            int offset = 0;
            int chr;
            while ((chr = stream.ReadByte()) > 0)
            {
                lineBuffer[offset] = (byte)chr;
                if (chr == '\n')
                {
                    if (offset < 1 || lineBuffer[offset - 1] != '\r')
                        throw new Exception("multipart/form-data content head read error, unformed");

                    if (offset == 1)
                        return "";

                    return Encoding.UTF8.GetString(lineBuffer, 0, offset - 1);
                }
                offset++;
                if (offset >= lineBuffer.Length)
                    throw new Exception("multipart/form-data content head read error, exceeds");
            }
            //连接丢失
            return null;
        }
    }
}
