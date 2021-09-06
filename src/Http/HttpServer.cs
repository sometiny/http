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
    public class HttpServer : HttpServerBase
    {
        public HttpServer() : base()
        {
            //注册一些路由
            RegisterRoute("/", OnIndex);
            RegisterRoute("/favicon.ico", OnFavicon);
            RegisterRoute("/post", OnReceivedPost);
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

    }
}
