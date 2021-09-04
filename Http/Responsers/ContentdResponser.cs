using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IocpSharp.Http.Responsers
{
    /// <summary>
    /// 固定长度内容的应答器
    /// </summary>
    public class ContentdResponser : HttpResponser
    {
        private int _contentLength = 0;
        private int _contentLengthWritten = 0;

        /// <summary>
        /// 使用固定长度和200响应代码实例化响应器
        /// </summary>
        /// <param name="contentLength">响应内容长度</param>
        public ContentdResponser(int contentLength) : this(contentLength, 200)
        {
        }

        /// <summary>
        /// 使用固定长度和响应代码实例化响应器
        /// </summary>
        /// <param name="contentLength">响应内容长度</param>
        /// <param name="statusCode">响应代码</param>
        public ContentdResponser(int contentLength, int statusCode) : base(statusCode)
        {
            _contentLength = contentLength;
            this["Content-Length"] = _contentLength.ToString();
        }

        /// <summary>
        /// 继承父类方法，用于判断数据是否超写
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public override void Write(Stream stream, byte[] buffer, int offset, int size)
        {
            if (_contentLengthWritten + size > _contentLength)
                throw new Exception($"响应内容太多，响应内容长度必须为：{_contentLength}");

            _contentLengthWritten += size;

            base.Write(stream, buffer, offset, size);
        }

        public override void End(Stream stream)
        {
            if (_contentLengthWritten != _contentLength)
                throw new Exception($"响应内容太少，响应内容长度必须为：{_contentLength}");
            WriteHeader(stream);
        }
    }
}
