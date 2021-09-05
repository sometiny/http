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

namespace IocpSharp.Http
{
    public class HttpServer : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            Stream stream = new BufferedNetworkStream(client, true);

            //捕获一个HttpRequest
            HttpRequest request = HttpRequest.Capture(stream);

            bool handled = false;

            if (request.Method == "GET")
                handled = ProcessGet(request, stream);

            if (request.Method == "POST")
                handled = ProcessPost(request, stream);

            //请求没被处理，返回个404
            if (!handled)
            {
                HttpResponser responser = new ChunkedResponser(404);

                responser.KeepAlive = false;
                responser.ContentType = "text/html; charset=utf-8";

                responser.Write(stream, "404 页面不存在");

                responser.End(stream);
            }

            stream.Close();
        }

        private bool ProcessGet(HttpRequest request, Stream stream)
        {
            //仅处理路径：/
            //返回个简单的表单给客户端
            if (request.Path == "/")
            {
                HttpResponser responser = new ChunkedResponser();

                responser.KeepAlive = false;
                responser.ContentType = "text/html; charset=utf-8";

                responser.Write(stream, "<form method=\"POST\" action=\"/post\">");
                responser.Write(stream, "姓名：<input type=text name=name /> <br />");
                responser.Write(stream, "年龄：<input type=text name=age /> <br />");
                responser.Write(stream, "<input type=submit value=提交 /> ");
                responser.Write(stream, "</form>");

                responser.End(stream);
                return true;
            }
            return false;
        }

        private bool ProcessPost(HttpRequest request, Stream stream)
        {
            //仅处理路径：/post
            if (request.Path == "/post")
            {
                byte[] entityContent = null;

                //如果浏览器传从数据了
                //打开一个读取流，然后把数据拷贝到内存流
                if (request.HasEntityBody)
                {
                    using(MemoryStream output  = new MemoryStream())
                    {
                        using(Stream input = request.OpenRead(stream))
                        {
                            input.CopyTo(output);
                        }
                        entityContent = output.ToArray();
                    }
                }


                //展示下客户端请求的一些东西
                HttpResponser responser = new ChunkedResponser();

                responser.KeepAlive = false;
                responser.ContentType = "text/html; charset=utf-8";

                responser.Write(stream, "<style type=\"text/css\">body{font-size:14px;}</style>");
                responser.Write(stream, "<h4>Hello World!</h4>");
                responser.Write(stream, $"Host Name: {request.Headers["host"]} <br />");
                responser.Write(stream, $"Method: {request.Method} <br />");
                responser.Write(stream, $"Request Url: {request.Url} <br />");
                responser.Write(stream, $"HttpPrototol: {request.HttpProtocol} <br />");
                responser.Write(stream, $"Time Now: {DateTime.Now: yyyy-MM-dd HH:mm:ss} <br /><br />");

                if (entityContent != null)
                {
                    responser.Write(stream, $"POST内容：" + Encoding.UTF8.GetString(entityContent));
                }


                responser.End(stream);
                return true;
            }
            return false;
        }
    }
}
