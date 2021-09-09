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
            HttpResponser responser = new ChunkedResponser();

            responser.ContentType = "text/html; charset=utf-8";
            responser.Write(stream, "<style type=\"text/css\">body{font-size:12px;}</style>");
            responser.Write(stream, "<h4>上传表单演示</h4>");
            responser.Write(stream, $"<a href=\"/index.html\">返回</a><br />");

            responser.Write(stream, $"ContentType：{request.ContentType}<br />");
            responser.Write(stream, $"Boundary：{request.Boundary}<br />");


            #region 输出解析后的上传内容
            responser.Write(stream, $"<h5>上传表单数据：</h5>");
            foreach (string formName in request.Form.Keys)
            {
                responser.Write(stream, $"{formName}: {request.Form[formName]}<br />");
            }

            responser.Write(stream, $"<h5>上传文件列表：</h5>");
            foreach (FileItem file in request.Files)
            {
                responser.Write(stream, $"{file.Name}: {file.FileName}, {file.TempFile}<br />");
            }
            #endregion

            #region 输出解析前的上传内容，不能同时与上面代码块运行
            //responser.Write(stream, $"<pre style=\"font-family:'microsoft yahei',arial; color: green\">{Encoding.UTF8.GetString( request.RequestBody)}</pre>");
            #endregion

            responser.End(stream);

            return true;
        }
    }
}
