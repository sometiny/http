using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http;
using IocpSharp.WebSocket;
using IocpSharp.WebSocket.Frames;

namespace IocpSharp.WebSocket
{
    /// <summary>
    /// 封装WebSocket帧解析
    /// </summary>
    public abstract class Messager
    {
        private Stream _baseStream = null;

        private bool _closeFrameSent = false;

        /// <summary>
        /// 使用基础流初始化Messager
        /// </summary>
        /// <param name="baseStream">基础流</param>
        public Messager(Stream baseStream)
        {
            _baseStream = baseStream;
        }
        /// <summary>
        /// 必须调用这个方法，开始处理数据
        /// </summary>
        internal void Accept()
        {
            try
            {
                OnConnected();
                ProcessFrame();
            }
            catch
            {
                _baseStream?.Close();
            }
        }

        /// <summary>
        /// 客户端连接进来
        /// </summary>
        protected virtual void OnConnected() {}

        /// <summary>
        /// 客户端断开
        /// </summary>
        protected virtual void OnDisconnected() {}

        /// <summary>
        /// 接收到文本消息
        /// </summary>
        /// <param name="payload">文本内容</param>
        protected virtual void OnText(string payload) {}

        /// <summary>
        /// 接收帧，不建议在重载方法中写收发逻辑，仅调试用。
        /// </summary>
        /// <param name="frame">帧</param>
        protected virtual void OnNewFrame(Frame frame) { }

        /// <summary>
        /// 接收到二进制消息
        /// </summary>
        /// <param name="inputStream">二进制输入流</param>
        protected virtual void OnBinary(Stream inputStream){}

        /// <summary>
        /// 发送关闭帧给客户端
        /// </summary>
        /// <param name="code">状态码</param>
        /// <param name="reason">原因</param>
        public void Close(int code, string reason)
        {
            _closeFrameSent = true;
            CloseFrame response = new CloseFrame(code, reason);
            response.OpenWrite(_baseStream);
        }

        /// <summary>
        ///  发送关闭帧给客户端，使用默认状态码1000
        /// </summary>
        public void Close()
        {
            Close(1000, null);
        }

        /// <summary>
        /// 发送文本
        /// </summary>
        /// <param name="message">文本内容</param>
        public void Send(string message)
        {
            TextFrame response = new TextFrame(message);
            response.OpenWrite(_baseStream);
        }

        /// <summary>
        /// 发送二进制数据到客户端
        /// </summary>
        /// <param name="binary">数据</param>
        public void Send(byte[] binary)
        {
            Frame frame = new Frame() { Fin = true, PayloadLength = binary.Length };
            using Stream output = frame.OpenWrite(_baseStream);
            output.Write(binary, 0, binary.Length);
        }

        /// <summary>
        /// 发送流到客户端
        /// </summary>
        /// <param name="input">输入流</param>
        public void Send(Stream input)
        {
            Frame frame = new Frame() { Fin = true, PayloadLength = input.Length - input.Position };
            using Stream output = frame.OpenWrite(_baseStream);
            input.CopyTo(output);
        }

        /// <summary>
        /// 发送Ping帧
        /// </summary>
        public void Ping()
        {
            PingFrame frame = new PingFrame(Frame.RandomBytes(16));
            frame.OpenWrite(_baseStream);
        }


        /// <summary>
        /// 接收到Close消息
        /// 如果之前发送过Close帧，则忽略
        /// </summary>
        /// <param name="code">状态码</param>
        /// <param name="reason">原因</param>
        private void OnCloseInternal(int code, string reason)
        {
            if (code > 1000 || _closeFrameSent) return;

            _closeFrameSent = true;
            CloseFrame response = new CloseFrame(code == 0 ? 1000 : code, code == 0 ? "established fulfilled" : reason);
            response.OpenWrite(_baseStream);
        }

        /// <summary>
        /// 接收到Ping消息，自动回复Pong
        /// </summary>
        /// <param name="payload"></param>
        private void OnPingInternal(byte[] payload)
        {
            PongFrame response = new PongFrame(payload);
            response.OpenWrite(_baseStream);
        }

        private void ProcessFrame()
        {
            while (true)
            {
                Frame frame;
                try
                {
                    frame = Frame.NextFrame(_baseStream);
                }
                catch (IOException)
                {
                    _baseStream.Close();
                    OnDisconnected();
                    return;
                }
                OnNewFrame(frame);
                //读出所有Payload
                byte[] payload = null;
                if (frame.OpCode != OpCode.Binary)
                {
                    using Stream input = Frame.OpenRead(frame, _baseStream);
                    payload = StreamUtils.ReadAllBytes(input);
                }

                //收到关闭帧，需要必要情况下需要向客户端回复一个关闭帧。
                //关闭帧比较特殊，客户端可能会发送状态码或原因给服务器
                //可以从payload里面把状态码和原因分析出来
                //前两个字节位状态码，unsigned int；紧跟着状态码的是原因。
                if (frame.OpCode == OpCode.Close)
                {
                    int code = 0;
                    string reason = null;

                    if (payload.Length >= 2)
                    {
                        code = payload[0] << 8 | payload[1];
                        reason = Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
                    }

                    OnCloseInternal(code, reason);
                    break;
                }

                if (frame.OpCode == OpCode.Ping)
                {
                    OnPingInternal(payload);
                    continue;
                }

                if (frame.OpCode == OpCode.Pong)
                {
                    continue;
                }

                if (frame.OpCode == OpCode.Binary)
                {
                    using Stream input = Frame.OpenRead(frame, _baseStream);
                    OnBinary(input);
                    ///把流清空，确保没有脏数组
                    StreamUtils.Clear(input);
                    continue;
                }

                if (frame.OpCode == OpCode.Text)
                {
                    OnText(Encoding.UTF8.GetString(payload));
                    continue;
                }

                //收到其他任何OpCode，退出，借用RFC原文
                //1003 indicates that an endpoint is terminating the connection
                //because it has received a type of data it cannot accept(e.g., an
                //endpoint that understands only text data MAY send this if it
                //receives a binary message).
                OnCloseInternal(1003, "Unknown Message Type");
                break;
            }
            _baseStream.Close();
            OnDisconnected();
        }
    }
}
