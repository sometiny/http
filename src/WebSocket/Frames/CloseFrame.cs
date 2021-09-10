using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.WebSocket.Frames
{
    /// <summary>
    /// 关闭帧。服务端收到关闭帧后，需要向客户端回复一个关闭帧
    /// 状态码可参考：https://developer.mozilla.org/en-US/docs/Web/API/CloseEvent/code
    /// </summary>
    public class CloseFrame : Frame
    {
        private int code_ = 1000;
        private byte[] reason_ = new byte[0];
        public CloseFrame(int code, string reason)
        {
            code_ = code;
            reason_ = Encoding.UTF8.GetBytes(reason ?? "");

            Fin = true;
            OpCode = OpCode.Close;
            Mask = false;
            if (code_ == 0)
            {
                PayloadLength = 0;
                return;
            }
            PayloadLength = 2 + reason_.Length;
        }

        /// <summary>
        /// 重载，将Close帧发送给客户端
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>返回值为null</returns>
        public override Stream OpenWrite(Stream stream)
        {
            using (Stream input = base.OpenWrite(stream))
            {
                if (code_ == 0) return null;

                byte[] buffer = new byte[2 + reason_.Length];
                buffer[0] = (byte)((code_ >> 8) & 0xff);
                buffer[1] = (byte)(code_ & 0xff);
                reason_.CopyTo(buffer, 2);

                input.Write(buffer, 0, buffer.Length);
            }
            return null;
        }
    }
}
