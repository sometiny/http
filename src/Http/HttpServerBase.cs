using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Responsers;
using IocpSharp.Http.Streams;
using System.IO.Compression;
using IocpSharp.Server;
using IocpSharp.Http.Utils;

namespace IocpSharp.Http
{
    //我们独立出一个基类来，以后新的服务继承本类就好
    public class HttpServerBase : TcpIocpServer
    {
        private static int MaxRequestPerConnection = 20;

        private string _webRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"));

        public string WebRoot { get => _webRoot; set => _webRoot = value; }

        //后面的代码可能会越来越复杂，我们做个简单的路由功能
        //可以开发功能更强大的路由
        private Dictionary<string, Func<HttpRequest, Stream, bool>> _routes = new Dictionary<string, Func<HttpRequest, Stream, bool>>();

        public HttpServerBase() : base()
        {
        }
        protected override void Start()
        {
            if (!Directory.Exists(_webRoot)) throw new Exception($"网站根目录不存在，请手动创建：{_webRoot}");

            base.Start();
        }

        public void RegisterRoute(string path, Func<HttpRequest, Stream, bool> route)
        {
            _routes[path] = route;
        }

        protected override void NewClient(Socket client)
        {
            Stream stream = new BufferedNetworkStream(client, true);

            //设置每个链接能处理的请求数
            int processedRequest = 0;
            while (processedRequest < MaxRequestPerConnection)
            {
                HttpRequest request = null;
                try
                {
                    //捕获一个HttpRequest
                    request = HttpRequest.Capture(stream);

                    //尝试查找路由，不存在的话使用NotFound路由
                    if (!_routes.TryGetValue(request.Path, out Func<HttpRequest, Stream, bool> handler))
                    {
                        //未匹配到路由，统一当文件资源处理
                        handler = OnResource;
                    }

                    //如果处理程序返回false，那么我们退出循环，关掉连接。
                    if (!handler(request, stream)) break;

                    //确保当前请求的请求实体读取完毕
                    request.EnsureEntityBodyRead();
                    //释放掉当前请求，准备下一次请求
                    processedRequest++;
                }
                catch (HttpRequestException e)
                {
                    if (e.Error == HttpRequestError.ConnectionLost) break;

                    //客户端发送的请求异常
                    OnBadRequest(stream, $"请求异常：{e.Error}");
                    break;

                }
                catch (Exception e)
                {
                    //其他异常
                    OnServerError(stream, $"请求异常：{e}");
                    break;
                }
                finally
                {
                    //始终释放请求
                    request?.Dispose();
                }
            }
            stream.Close();
        }

        /// <summary>
        /// 响应404错误
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual bool OnNotFound(HttpRequest request, Stream stream)
        {
            HttpResponser responser = new ChunkedResponser(404);
            responser.KeepAlive = false;
            responser.ContentType = "text/html; charset=utf-8";
            responser.Write(stream, $"请求的资源'{request.Path}'不存在。");
            responser.End(stream);
            return false;
        }


        /// <summary>
        /// 请求异常
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        protected virtual void OnBadRequest(Stream stream, string message)
        {
            HttpResponser responser = new ChunkedResponser(400);
            responser.KeepAlive = false;
            responser.ContentType = "text/html; charset=utf-8";
            responser.Write(stream, message);
            responser.End(stream);
        }

        /// <summary>
        /// 服务器异常
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        protected virtual void OnServerError(Stream stream, string message)
        {
            HttpResponser responser = new ChunkedResponser(500);
            responser.KeepAlive = false;
            responser.ContentType = "text/html; charset=utf-8";
            responser.Write(stream, message);
            responser.End(stream);
        }

        /// <summary>
        /// 发送服务器资源，这里简单处理下。
        /// 必要的情况下可以作缓存处理
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        protected virtual bool OnResource(HttpRequest request, Stream stream)
        {
            string path = request.Path;

            ///处理下非安全的路径
            if (path.IndexOf("..") >= 0 || !path.StartsWith("/"))
            {
                throw new HttpRequestException(HttpRequestError.ResourcePathError, "不安全的路径访问");
            }


            string filePath = Path.GetFullPath(Path.Combine(_webRoot, "." + path));

            FileInfo fileInfo = new FileInfo(filePath);
            string mimeType = MimeTypes.GetMimeType(fileInfo.Extension);

            if (string.IsNullOrEmpty(mimeType))
            {
                throw new HttpRequestException(HttpRequestError.ResourceMimeError, "不支持的文件类型");
            }

            if (!fileInfo.Exists)
            {
                return OnNotFound(request, stream);
            }

            HttpResponser responser = new HttpResponser();

            //拿到的MIME输出给客户端
            responser.ContentType = mimeType;
            responser.ContentLength = fileInfo.Length;

            using (Stream output = responser.OpenWrite(stream))
            {
                using(Stream input = fileInfo.OpenRead())
                {
                    input.CopyTo(stream);
                }
            }
            return true;
        }

    }
}
