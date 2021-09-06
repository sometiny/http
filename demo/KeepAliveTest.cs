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
    public class KeepAliveTest : TcpIocpServer
    {
        private static int MaxRequestPerConnection = 20;

        //后面的代码可能会越来越复杂，我们做个简单的路由功能
        //可以开发功能更强大的路由
        private Dictionary<string, Func<HttpRequest, Stream, bool>> _routes = new Dictionary<string, Func<HttpRequest, Stream, bool>>();

        public KeepAliveTest() : base()
        {
            //注册一些路由
            _routes["/"] = new Func<HttpRequest, Stream, bool>(OnIndex);
            _routes["/favicon.ico"] = new Func<HttpRequest, Stream, bool>(OnFavicon);
            _routes["/post"] = new Func<HttpRequest, Stream, bool>(OnReceivedPost);
            _routes["*"] = new Func<HttpRequest, Stream, bool>(OnNotFound);
        }

        protected override void NewClient(Socket client)
        {
            //控制台输出下，跟踪下新连接
            Console.WriteLine($"New Client: {client.RemoteEndPoint}");

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

                    //控制台输出，跟踪下新请求
                    Console.WriteLine($"New Request: {request.Method} {request.Url}");

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
        /// 首页路由处理程序
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        private bool OnIndex(HttpRequest request, Stream stream)
        {
            //展示下客户端请求的一些东西
            HttpResponser responser = new ChunkedResponser();

            responser.ContentType = "text/html; charset=utf-8";

            responser.Write(stream,
@"<form method=""POST"" action=""/post?action=save"" >
姓名：<input type=text name=name value=""测试hello world!"" /> <br />
年龄：<input type=text name=age value=31 /> <br />
<input type=submit value=提交 /> 
</form>");
            responser.End(stream);

            return true;
        }

        /// <summary>
        /// 处理POST数据
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool OnReceivedPost(HttpRequest request, Stream stream)
        {
            //展示下客户端请求的一些东西
            HttpResponser responser = new ChunkedResponser();

            responser.ContentType = "text/html; charset=utf-8";

            responser.Write(stream, "<style type=\"text/css\">body{font-size:14px;}</style>");
            responser.Write(stream, "<h4>Hello World!</h4>");
            responser.Write(stream, $"<a href=\"/\">返回</a><br />");
            responser.Write(stream, $"Host Name: {request.Headers["host"]} <br />");
            responser.Write(stream, $"Method: {request.Method} <br />");
            responser.Write(stream, $"Request Url: {request.Url} <br />");
            responser.Write(stream, $"HttpPrototol: {request.HttpProtocol} <br />");
            responser.Write(stream, $"Time Now: {DateTime.Now: yyyy-MM-dd HH:mm:ss} <br /><br />");

            //输出解析后的查询字符串数据
            responser.Write(stream, $"查询参数解析结果：<br />");
            var queryString = request.QueryString;
            foreach (string name in queryString.Keys)
            {
                responser.Write(stream, $"&nbsp; &nbsp; {name} = {queryString[name]}<br />");
            }

            //输出解析后的form表单数据
            var form = request.Form;
            responser.Write(stream, $"POST解析结果：<br />");
            foreach (string name in form.Keys)
            {
                responser.Write(stream, $"&nbsp; &nbsp; {name} = {form[name]}<br />");
            }

            responser.End(stream);

            return true;
        }

        /// <summary>
        /// 输出favicon.ico给浏览器用
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        private bool OnFavicon(HttpRequest request, Stream stream)
        {
            HttpResponser responser = new HttpResponser();

            string iconPath = AppDomain.CurrentDomain.BaseDirectory + "favicon.ico";
            if (!File.Exists(iconPath))
            {
                return OnNotFound(request, stream);
            }

            byte[] iconData = File.ReadAllBytes(iconPath);

            responser.ContentType = "image/vnd.microsoft.icon";
            responser.ContentLength = iconData.Length;

            responser.Write(stream, iconData);
            return true;
        }

        /// <summary>
        /// 响应404错误
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool OnNotFound(HttpRequest request, Stream stream)
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
        private void OnBadRequest(Stream stream, string message)
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
        private void OnServerError(Stream stream, string message)
        {
            HttpResponser responser = new ChunkedResponser(500);
            responser.KeepAlive = false;
            responser.ContentType = "text/html; charset=utf-8";
            responser.Write(stream, message);
            responser.End(stream);
        }

    }
}
