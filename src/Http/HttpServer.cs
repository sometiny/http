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
            //设置Web根目录
            //方便输出静态文件
            WebRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"));
            UplaodTempDir = AppDomain.CurrentDomain.BaseDirectory + "uploads";
            //注册一些路由
            RegisterRoute("/", OnIndex);
            RegisterRoute("/post", OnReceivedPost);
        }
        /// <summary>
        /// 首页路由处理程序
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        private bool OnIndex(HttpRequest request, Stream stream)
        {
            //跳转到页面
            HttpResponser responser = new ChunkedResponser(301);
            responser.ContentType = "text/html; charset=utf-8";
            responser["Location"] = "/index.html";
            responser.Write(stream, "Redirect To '/index.html'");
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
            responser.Write(stream, $"<a href=\"/index.html\">返回</a><br />");
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

            responser.Write(stream, $"ContentType：{request.ContentType}<br />");
            responser.Write(stream, $"Boundary：{request.Boundary}<br />");


            var parser = new HttpMultipartFormDataParser(UplaodTempDir);
            parser.Parse(request.OpenRead(), request.Boundary);


            var forms = parser.Forms;
            var files = parser.Files;
            responser.Write(stream, $"上传的表单：<br />");

            foreach (string key in forms.Keys)
            {
                responser.Write(stream, $"&nbsp; &nbsp; {key}：{forms[key]}<br />");
            }

            responser.Write(stream, $"上传的文件：<br />");

            foreach (FileItem file in files)
            {
                responser.Write(stream, $"&nbsp; &nbsp; {file.Name}：{file.FileName}, {file.TempFile}<br />");
            }

            responser.End(stream);

            return true;
        }
    }
}
