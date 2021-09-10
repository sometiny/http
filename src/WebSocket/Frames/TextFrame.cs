using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IocpSharp.WebSocket.Frames
{
    public class TextFrame : Frame
    {
        private byte[] _payload = new byte[0];
        public TextFrame()
        {
            Fin = true;
            OpCode = OpCode.Text;
            Mask = false;
            PayloadLength = 0;
        }
        public TextFrame(string payload) : this(Encoding.UTF8.GetBytes(payload))
        {
        }

        public TextFrame(byte[] payload) : this()
        {
            _payload = payload;
            PayloadLength = _payload.Length;
        }

        /// <summary>
        /// 将Text帧发送给客户端
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>返回值为null</returns>
        public override Stream OpenWrite(Stream stream)
        {
            using (Stream input = base.OpenWrite(stream))
            {
                if (_payload.Length == 0) return null;
                input.Write(_payload, 0, _payload.Length);
            }
            return null;
        }
    }
}
