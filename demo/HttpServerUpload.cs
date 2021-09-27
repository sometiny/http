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
    public class HttpServerUplaod : HttpServerBase
    {
        public HttpServerUplaod() : base()
        {
            //设置Web根目录
            //方便输出静态文件
            WebRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"));
            UplaodTempDir = AppDomain.CurrentDomain.BaseDirectory + "uploads";
            //注册一些路由
            RegisterRoute("/", OnIndex);
            RegisterRoute("/post", OnReceivedPost);
        }

        protected override void NewClient(Socket client)
        {
            Console.WriteLine($"New Client: {client.RemoteEndPoint}");
            base.NewClient(client);
        }

        /// <summary>
        /// 首页路由处理程序
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        private void OnIndex(HttpRequest request)
        {
            Next(request, new HttpRedirectResponser("/index.html", "Redirect To '/index.html'", 302));
        }

        /// <summary>
        /// 处理POST数据
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private void OnReceivedPost(HttpRequest request)
        {
            HttpResponser response = HttpResponser.Create(request);

            response.ContentType = "text/html; charset=utf-8";

            response.Write("<style type=\"text/css\">body{font-size:12px;}</style>");
            response.Write("<h4>上传表单演示</h4>");
            response.Write($"<a href=\"/index.html\">返回</a><br />");

            response.Write($"ContentType：{request.ContentType}<br />");
            response.Write($"Boundary：{request.Boundary}<br />");


            response.Write($"<h5>上传表单数据：</h5>");
            foreach (string formName in request.Form.Keys)
            {
                response.Write($"{formName}: {request.Form[formName]}<br />");
            }

            response.Write($"<h5>上传文件列表：</h5>");
            foreach (FileItem file in request.Files)
            {
                response.Write($"{file.Name}: {file.FileName}, {file.TempFile}<br />");
            }
            Next(request, response);
        }
    }
}
