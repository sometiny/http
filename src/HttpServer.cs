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
using IocpSharp.WebSocket;
using IocpSharp.WebSocket.Frames;
using System.Net;

namespace IocpSharp.Http
{
    public class HttpServer : HttpServerBase
    {
        public HttpServer() : base()
        {
            //设置根目录
            WebRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"));
        }
        protected override void OnWebSocket(HttpRequest request, Stream stream)
        {
            new MyMessager(stream).Accept();
        }
    }

    public class MyMessager : Messager
    {
        private EndPoint _remoteEndPoint = null;
        public MyMessager(Stream stream) : base(stream) {
            if(stream is BufferedNetworkStream networkStream)
            {
                _remoteEndPoint = networkStream.BaseSocket.RemoteEndPoint ;
            }
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 新客户端连接：{_remoteEndPoint}");
        }
        protected override void OnDisconnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 连接断开：{_remoteEndPoint}");
        }
        protected override void OnNewFrame(Frame frame)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 接收到新帧；帧类型：{frame.OpCode}，结束帧：{frame.Fin}，携带掩码：{frame.Mask}，长度：{frame.PayloadLength}");
            Console.ForegroundColor = color;
        }

        protected override void OnText(string payload)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 文本数据：{payload}");
            if(payload == "close")
            {
                Send($"服务器接收到close指令，关闭连接。");
                Close();
                return;
            }
            if (payload == "ping")
            {
                Send($"服务器接收到ping指令，发送ping。");
                Ping();
                return;
            }
            Send($"服务器接收到文本数据：{payload}");
        }

        protected override void OnBinary(Stream inputStream)
        {
            byte[] payload = StreamUtils.ReadAllBytes(inputStream);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 二进制数据，长度：{payload.Length}");
            Send($"服务器接收到二进制数据，长度：{payload.Length}");
        }
    }
}
