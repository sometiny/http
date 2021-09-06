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
    public class PostAndQueryString : TcpIocpServer
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

                responser.Write(stream,
@"<form method=""POST"" action=""/post?action=save"" >
姓名：<input type=text name=name value=""测试hello world!"" /> <br />
年龄：<input type=text name=age value=31 /> <br />
<input type=submit value=提交 /> 
</form>");

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
                //这个数据的获取，可以放在HttpRequest实现
                if (request.HasEntityBody)
                {
                    using(MemoryStream output  = new MemoryStream())
                    {
                        using(Stream input = request.OpenRead())
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


                responser.Write(stream, $"查询参数：{request.Query}<br />");
                responser.Write(stream, $"查询参数解析结果：<br />");

                var queryString = request.QueryString;
                foreach (string name in queryString.Keys)
                {
                    responser.Write(stream, $"&nbsp; &nbsp; {name} = {queryString[name]}<br />");
                }

                if (entityContent != null)
                {
                    //请求实体的原文本数据
                    string formString = Encoding.UTF8.GetString(entityContent);

                    //解析文本为NameValueCollection
                    var form = HttpUtility.ParseUriComponents(formString);

                    //输出原文
                    responser.Write(stream, $"POST原内容：{formString}<br />");

                    //输出解析后的数据
                    responser.Write(stream, $"POST解析结果：<br />");
                    foreach (string name in form.Keys)
                    {
                        responser.Write(stream, $"&nbsp; &nbsp; {name} = {form[name]}<br />");
                    }

                }


                responser.End(stream);
                return true;
            }
            return false;
        }
    }
}
