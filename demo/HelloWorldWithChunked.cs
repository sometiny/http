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
    public class HelloWorldWithChunked : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            HttpStream stream = new HttpStream(new NetworkStream(client, true), false);
            //捕获一个HttpRequest
            HttpRequest request = stream.Capture<HttpRequest>();

            //实例化一个Chunked模式的应答器
            HttpResponser responser = new ChunkedResponser();

            //发送个Server标头给客户端，验证下标头是否正确
            responser.Server = "IServer/1.0";

            responser.ContentType = "text/html";
            responser.KeepAlive = false;

            //发送响应内容
            //ChunkedResponser的Write方法会对数据进行Chunked封装
            //可以多次调用Write向客户端发送数据
            responser.Write(stream, "<p>hello world!</p>");
            responser.Write(stream, $"<pre>{request.GetAllRequestHeaders()}</pre>");

            //必须调用End方法，向客户端发送结束包
            responser.End(stream);

            stream.Close();
        }
    }
}
