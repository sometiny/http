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
    public class HelloWorld : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            Stream stream = new NetworkStream(client, true);

            //捕获一个HttpRequest
            HttpRequest request = HttpRequest.Capture(stream);

            //实例化HttpResponse，方便管理HTTP响应头
            HttpResponse response = new HttpResponse(200);

            //需要发送到客户端的内容
            string responseText = "<p>hello world!</p>";

            //内容对应的二进制数据
            byte[] responseBody = Encoding.ASCII.GetBytes(responseText);

            //设置Content-Length请求头，大小为响应内容的二进制数据长度
            //由于HTTP/1.1协议的限制，客户端必须有明确的界定条件，来从传输中读取正确的内容
            //一种方法就是，设置Content-Length头，客户端只要读取指定长度的数据即可。
            //再一种方法就是，使用Chunked，进行分块传输，用一个结束快告诉客户端，数据传输完毕了
            //还有就是，服务器发送完数据后，可以直接关闭连接，客户端读完全部数据。
            //
            //对于一些不需要响应内容的code，可以不设置。例如204 No Content
            response.SetHeader("Content-Length", responseBody.Length.ToString());

            //设置响应内容类型，这里设置为text/html
            response.SetHeader("Content-Type", "text/html");

            //告诉浏览器，本次连接为非长链，服务器会关闭连接。
            //HTTP/1.1允许长链接，可以在一个连接上发送多次请求和响应。
            response.SetHeader("Connection", "close");

            //获取整个需要发送给客户端的响应头
            string responseHeaders = response.GetAllResponseHeaders();
            byte[] responseHeaderBuffer = Encoding.ASCII.GetBytes(responseHeaders);

            //发送响应头
            stream.Write(responseHeaderBuffer, 0, responseHeaderBuffer.Length);

            //发送响应内容
            stream.Write(responseBody, 0, responseBody.Length);

            stream.Close();
        }
    }
}
