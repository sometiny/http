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
            while (true)
            {
                Frame frame = null;
                try
                {
                    frame = Frame.NextFrame(stream);
                }
                catch (IOException)
                {
                    Console.WriteLine("客户端连接断开，可能是强制刷新了页面，关闭了socket，而没有发送Close帧。");
                    break;
                }
                Console.WriteLine($"帧类型：{frame.OpCode}，是否有掩码：{frame.Mask}，帧长度：{frame.PayloadLength}");

                byte[] payload = null;
                using (Stream input = Frame.OpenRead(frame, stream))
                {
                    using MemoryStream output = new MemoryStream();
                    input.CopyTo(output);
                    payload = output.ToArray();
                }

                //收到关闭帧，需要必要情况下需要向客户端回复一个关闭帧。
                //关闭帧比较特殊，客户端可能会发送状态码或原因给服务器
                //可以从payload里面把状态码和原因分析出来
                //前两个字节位状态码，short；紧跟着状态码的是原因。
                if (frame.OpCode == OpCode.Close)
                {
                    int code = 0;
                    string reason = null;

                    if(payload.Length >= 2) {
                        code = payload[0] << 8 | payload[1];
                        reason = Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
                    }
                    Console.WriteLine($"关闭原因：{code}，{reason}");

                    //正常关闭WebSocket，回复关闭帧
                    //其他Code直接退出循环关闭基础流
                    if (code <= 1000)
                    {
                        CloseFrame response = new CloseFrame(code, reason);
                        response.OpenWrite(stream);
                    }
                    break;
                }

                //收到Ping帧，需要向客户端回复一个Pong帧。
                //如果有payload，同时发送给客户端
                if (frame.OpCode == OpCode.Ping)
                {
                    PongFrame response = new PongFrame(payload);
                    response.OpenWrite(stream);
                    continue;
                }

                //收到Binary帧，打印下内容
                //这里可以使用流的方式，把帧数据保存到文件或其他应用
                if(frame.OpCode == OpCode.Binary)
                {
                    Console.WriteLine(string.Join(", ", payload));

                    //为了测试，我们随便发送测试内容给客户端
                    TextFrame response = new TextFrame($"服务器收到二进制数据，长度：{payload.Length}");
                    response.OpenWrite(stream);
                    continue;
                }

                //收到文本，打印出来
                if (frame.OpCode == OpCode.Text)
                {
                    string message = Encoding.UTF8.GetString(payload);
                    Console.WriteLine(message);

                    //为了测试，我们把信息再发回客户端
                    TextFrame response = new TextFrame($"服务器接收到文本数据：{message}");
                    response.OpenWrite(stream);
                }

            }
            stream.Close();
        }
    }
}
