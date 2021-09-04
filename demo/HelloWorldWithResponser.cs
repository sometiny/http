using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Responsers;
using System.IO.Compression;
using IocpSharp.Server;

namespace IocpSharp.Http
{
    public class HelloWorldWithResponser : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            Stream stream = new NetworkStream(client, true);

            //捕获一个HttpRequest
            HttpRequest request = HttpRequest.Capture(stream);


            //需要发送到客户端的内容
            string responseText = $"<p>hello world!</p><pre>{request.GetAllRequestHeaders()}</pre>";

            //内容对应的二进制数据
            byte[] responseBody = Encoding.ASCII.GetBytes(responseText);

            //实例化一个应答器
            HttpResponser responser = new HttpResponser();

            //设置响应内容长度和类型，应答器对常用的标头作了快捷设置
            responser.ContentLength = responseBody.Length;
            responser.ContentType = "text/html";
            responser.KeepAlive = false;

            //发送响应内容
            responser.Write(stream, responseBody);
            responser.End(stream);


            stream.Close();
        }
    }
}
