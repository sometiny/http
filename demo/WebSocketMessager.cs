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
    public class WebSocketMessager : HttpServerBase
    {
        public WebSocketMessager() : base()
        {
            //设置根目录
            WebRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"));
        }
        protected override void OnWebSocket(HttpRequest request, Stream stream)
        {
            new WsMessager(stream).Accept();
        }
    }

    public class WsMessager : Messager
    {
        private EndPoint _remoteEndPoint = null;
        public WsMessager(Stream stream) : base(stream) {
            if(stream is BufferedNetworkStream networkStream)
            {
                _remoteEndPoint = networkStream.BaseSocket.RemoteEndPoint ;
            }
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 新客户端连接：{_remoteEndPoint}"  );
        }
        protected override void OnDisconnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 连接断开：{_remoteEndPoint}");
        }

        protected override void OnText(string payload)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 接收到文本数据：{payload}");
            Send($"服务器接收到文本数据：{payload}");
            if(payload == "close")
            {
                Close();
            }
        }

        protected override void OnBinary(Stream inputStream)
        {
            byte[] payload = StreamUtils.ReadAllBytes(inputStream);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 接收到二进制数据，长度：{payload.Length}");
            Send($"服务器接收到二进制数据，长度：{payload.Length}");
        }
    }
}
