using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IocpSharp.WebSocket
{
    /// <summary>
    /// 操作类型，帧类型
    /// </summary>
    public enum OpCode
    {
        /// <summary>
        /// 我也不知道该怎么“称呼他”
        /// </summary>
        Continuation = 0,

        /// <summary>
        /// 文本帧
        /// </summary>
        Text = 1,

        /// <summary>
        /// 二进制帧
        /// </summary>
        Binary = 2,

        /// <summary>
        /// 关闭帧
        /// </summary>
        Close = 8,

        /// <summary>
        /// Ping帧
        /// </summary>
        Ping = 9,

        /// <summary>
        /// Pong帧，响应Ping的。Ping、Pong、Ping、Pong、Ping、Pong。。。打球呢
        /// </summary>
        Pong = 0xa,
    }


    /// <summary>
    /// 帧，WebSocket消息的基本单位
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// 当前帧是不是结束帧，如果不是，必须继续读下一个帧，直到读到结束帧。
        /// </summary>
        public bool Fin { get; set; } = false;

        /// <summary>
        /// 保留字段，我们不用
        /// </summary>
        public bool Rsv1 { get; set; } = false;
        public bool Rsv2 { get; set; } = false;
        public bool Rsv3 { get; set; } = false;

        /// <summary>
        /// 帧类型
        /// </summary>
        public OpCode OpCode { get; set; } = OpCode.Text;

        /// <summary>
        /// 帧Payload是否带有掩码
        /// </summary>
        public bool Mask { get; set; } = false;

        /// <summary>
        /// 标识Payload长度的字节数，只可能是这三个值：0、2、8
        /// </summary>
        public int PayloadLengthBytesCount { get; set; } = 0;

        /// <summary>
        /// Payload长度
        /// </summary>
        public long PayloadLength { get; set; } = 0;

        /// <summary>
        /// 标识Payload长度的字节+掩码（4字节，如果有的话）
        /// </summary>
        public byte[] MaskKey { get; set; } = null;

        /// <summary>
        /// 掩码在MaskKey内的索引，掩码一定是最后四个字节
        /// </summary>
        public int MaskKeyOffset { get; set; } = 0;



        public static Frame NextFrame(Stream baseStream)
        {
            byte[] buffer = new byte[2];
            ReadPackage(baseStream, buffer, 0, 2);

            Frame frame = new Frame();

            //处理第一字节
            //第一位，如果为1，代表帧为结束帧
            frame.Fin = buffer[0] >> 7 == 1;

            //三个保留位，我们不用
            frame.Rsv1 = (buffer[0] >> 6 & 1) == 1;
            frame.Rsv2 = (buffer[0] >> 5 & 1) == 1;
            frame.Rsv3 = (buffer[0] >> 4 & 1) == 1;

            //5-8位，代表帧类型
            frame.OpCode = (OpCode)(buffer[0] & 0xf);

            //处理第二个字节
            //第一位，如果为1，代表Payload经过掩码处理
            frame.Mask = buffer[1] >> 7 == 1;

            //2-7位，Payload长度标识
            int payloadLengthMask = buffer[1] & 0x7f;

            //如果值小于126，那么这个值就代表的是Payload实际长度
            if (payloadLengthMask < 126)
            {
                frame.PayloadLength = payloadLengthMask;
            }
            //126代表紧跟着的2个字节保存了Payload长度
            else if (payloadLengthMask == 126)
            {
                frame.PayloadLengthBytesCount = 2;

            }
            //126代表紧跟着的8个字节保存了Payload长度，对，就是没有4个字节。
            else if (payloadLengthMask == 127)
            {
                frame.PayloadLengthBytesCount = 8;

            }

            //如果没有掩码,并且不需要额外的字节去确定Payload长度，直接返回
            //后面只要根据PayloadLength去读Payload即可
            if (!frame.Mask && frame.PayloadLengthBytesCount == 0)
            {
                return frame;
            }

            //把保存长度的2或8字节读出来即可
            //如果有掩码，需要继续读4个字节的掩码
            buffer = frame.Mask 
                ? new byte[frame.PayloadLengthBytesCount + 4] 
                : new byte[frame.PayloadLengthBytesCount];

            //读取Payload长度数据和掩码（如果有的话）
            ReadPackage(baseStream, buffer, 0, buffer.Length);

            //如果有掩码，提取出来
            if (frame.Mask)
            {
                frame.MaskKey = buffer.Skip(frame.PayloadLengthBytesCount).Take(4).ToArray();
            }

            //从字节数据中，获取Payload的长度
            if (frame.PayloadLengthBytesCount == 2)
            {
                frame.PayloadLength = buffer[0] << 8 | buffer[1];

            }
            else if (frame.PayloadLengthBytesCount == 8)
            {
                frame.PayloadLength = ToInt64(buffer);
            }

            //至此所有表示帧元信息的数据都被读出来
            //Payload的数据我们会用流的方式读出来
            //有些特殊帧，再Payload还会有特定的数据格式，后面单独介绍

            return frame;
        }

        /// <summary>
        /// 从流中读取指定长度的数据到缓冲区
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="size">长度</param>
        private static void ReadPackage(Stream source, byte[] buffer, int offset, int size)
        {

            int received = 0;
            int rec;
            while ((rec = source.Read(buffer, offset + received, size - received)) > 0)
            {
                received += rec;
                if (received == size) return;
            }
            if (received != size) throw new IOException("流被关闭，数据无法完整读取");
        }

        /// <summary>
        /// 字节数组转long
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static long ToInt64(byte[] buffer)
        {

            int num3 = (((buffer[0] << 0x18) | (buffer[1] << 0x10)) | (buffer[2] << 8)) | buffer[3];
            int num4 = (((buffer[4] << 0x18) | (buffer[5] << 0x10)) | (buffer[6] << 8)) | buffer[7];
            return (((long)((ulong)num4)) | (num3 << 0x20));

        }
    }
}
