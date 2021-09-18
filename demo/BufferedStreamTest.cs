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
    public class BufferedStreamTest : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            HttpStream stream = new HttpStream(new BufferedNetworkStream(client, true), false);
            //捕获一个HttpRequest
            HttpRequest request = stream.Capture<HttpRequest>();

            //懒得去计算数据长度，直接用Chunked方式发送数据
            HttpResponser responser = new ChunkedResponser();
            responser.ContentType = "text/html";
            responser.KeepAlive = false;

            responser.Write(stream, "<style type=\"text/css\">body{font-size:14px;}</style>");
            responser.Write(stream, "<h4>Hello World!</h4>");
            responser.Write(stream, $"Host Name: {request.Headers["host"]} <br />");
            responser.Write(stream, $"Method: {request.Method} <br />");
            responser.Write(stream, $"Request Url: {request.Url} <br />");
            responser.Write(stream, $"HttpPrototol: {request.HttpProtocol} <br />");
            responser.Write(stream, $"Time Now: {DateTime.Now: yyyy-MM-dd HH:mm:ss} <br />");

            responser.End(stream);

            stream.Close();
        }
    }
}
