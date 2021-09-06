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

        //后面的代码可能会越来越复杂，我们做个简单的路由功能
        //可以开发功能更强大的路由
        private Dictionary<string, Func<HttpRequest, Stream, bool>> _routes = new Dictionary<string, Func<HttpRequest, Stream, bool>>();

        public HttpServerBase() : base()
        {
            _routes["*"] = new Func<HttpRequest, Stream, bool>(OnNotFound);
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
                        handler = _routes["*"];
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

    }
}
