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
        protected override Messager GetMessager(HttpRequest request, Stream stream, EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
            return new WsMessager(stream, localEndPoint, remoteEndPoint);
        }
    }

    public class WsMessager : Messager
    {
        private EndPoint _remoteEndPoint = null;
        public WsMessager(Stream stream, EndPoint localEndPoint, EndPoint remoteEndPoint) : base(stream)
        {

            //获取客户端的连接信息
            _remoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// 客户端新连接
        /// </summary>
        protected override void OnConnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 新客户端连接：{_remoteEndPoint}");
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        protected override void OnDisconnected()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 连接断开：{_remoteEndPoint}");
        }

        /// <summary>
        /// 接收到新的帧，仅展示下
        /// </summary>
        /// <param name="frame"></param>
        protected override void OnNewFrame(Frame frame)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 接收到新帧；帧类型：{frame.OpCode}，结束帧：{frame.Fin}，携带掩码：{frame.Mask}，长度：{frame.PayloadLength}");
            Console.ForegroundColor = color;
        }

        /// <summary>
        /// 收到Text消息时的实现
        /// 里面定义两个特使的消息：close和ping，用来测试服务器主动发送Close和Ping帧。
        /// </summary>
        /// <param name="payload">完整消息</param>
        protected override void OnText(string payload)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 文本数据：{payload}");
            if (payload == "close")
            {
                Send($"服务器接收到close指令，关闭连接。");
                Close(1000, "client claim");
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

        /// <summary>
        /// 接收到二进制消息
        /// 二进制消息是通过流来读取的，不像Text，直接一股脑读取全部消息。
        /// </summary>
        /// <param name="inputStream">输入流</param>
        protected override void OnBinary(Stream inputStream)
        {
            //为了测试，我们把二进制消息读取到字节数组。
            byte[] payload = StreamUtils.ReadAllBytes(inputStream);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} > 二进制数据，长度：{payload.Length}");
            Send($"服务器接收到二进制数据，长度：{payload.Length}");
        }
    }
}
