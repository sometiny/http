using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IocpSharp.Http
{
    /// <summary>
    /// HTTP应答器，作为各种不同响应资源的父类
    /// </summary>
    public class HttpResponser
    {
        private HttpResponse _response = null;
        private bool _headerWritten = false;

        public HttpResponse Response => _response;

        public HttpResponser() : this(200) { }

        public HttpResponser(int statusCode) 
        {
            _response = new HttpResponse(statusCode);
            ///设定一些基本标头
            ///null值的标头不会被写入客户端
            ///提前将标头设置为null，可以确保标头写入客户端的顺序会按照设置的顺序来
            _response.Headers["Cache-Control"] = null;
            _response.Headers["Pragma"] = null;
            _response.Headers["Content-Type"] = null;
            _response.Headers["Expires"] = null;
            _response.Headers["Content-Type"] = null;
            _response.Headers["Content-Length"] = null;
            _response.Headers["Content-Encoding"] = null;
            _response.Headers["Content-Range"] = null;
            _response.Headers["Server"] = null;
            _response.Headers["X-Powered-By"] = null;
            _response.Headers["Location"] = null;
            _response.Headers["Date"] = DateTime.UtcNow.ToString("r");
            _response.Headers["Connection"] = null;

        }

        public string this[string name] { 
            get => _response.Headers[name]; 
            set => _response.Headers[name] = value;
        }

        public int ContentLength
        {
            set => _response.Headers["Content-Length"] = value.ToString();
        }
        public string Server
        {
            get => _response.Headers["Server"];
            set => _response.Headers["Server"] = value;
        }

        public string ContentType { 
            get => _response.Headers["Content-Type"]; 
            set => _response.Headers["Content-Type"] = value; 
        }
        public bool KeepAlive { 
            get => _response.Headers["Connection"] != "close"; 
            set => _response.Headers["Connection"] = value ? "keep-alive" : "close"; 
        }


        /// <summary>
        /// 向客户端写入响应头。
        /// 大部分情况下，在向客户端写入数据前会自动调用
        /// </summary>
        /// <param name="stream">要写入的数据流</param>
        public virtual void WriteHeader(Stream stream)
        {
            if (_headerWritten) return;

            _headerWritten = true;
            string responseHeaders = _response.GetAllResponseHeaders();
            byte[] responseHeaderBuffer = Encoding.ASCII.GetBytes(responseHeaders);

            //发送响应头
            stream.Write(responseHeaderBuffer, 0, responseHeaderBuffer.Length);
        }

        /// <summary>
        /// 向基础流写入文本，默认编码为UTF8
        /// </summary>
        /// <param name="stream">基础流</param>
        /// <param name="content">内容文本</param>
        public void Write(Stream stream, string content)
        {
            Write(stream, content, Encoding.UTF8);
        }

        /// <summary>
        /// 使用指定编码向基础流写入文本
        /// </summary>
        /// <param name="stream">基础流</param>
        /// <param name="content">内容文本</param>
        /// <param name="encoding">文本编码</param>
        public void Write(Stream stream, string content, Encoding encoding)
        {
            byte[] buffer = encoding.GetBytes(content);
            Write(stream, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 向基础流写入缓冲内容
        /// </summary>
        /// <param name="stream">基础流</param>
        /// <param name="buffer">缓冲内容</param>
        public void Write(Stream stream, byte[] buffer)
        {
            Write(stream, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 向基础流写入输入流
        /// </summary>
        /// <param name="stream">基础流</param>
        /// <param name="input">输入流</param>
        public void Write(Stream stream, Stream input)
        {
            byte[] buffer = new byte[65536];

            int rec;
            while ((rec = input.Read(buffer, 0, 65536)) > 0)
            {
                Write(stream, buffer, 0, rec);
            }
        }

        /// <summary>
        /// 向基础流写入缓冲内容
        /// </summary>
        /// <param name="stream">基础流</param>
        /// <param name="buffer">缓冲内容</param>
        /// <param name="offset">写入的数据在缓冲区的偏移</param>
        /// <param name="size">写入大小</param>
        public virtual void Write(Stream stream, byte[] buffer, int offset, int size)
        {
            WriteHeader(stream);
            stream.Write(buffer, offset, size);
        }

        /// <summary>
        /// 虚方法，可以确保响应头写到了浏览器
        /// 子类可以在重载中执行一些自己的逻辑
        /// 例如确保固定Content-Length的数据发送准确
        /// 确保Chunked传输的结束包发送
        /// </summary>
        public virtual void End(Stream stream)
        {
            WriteHeader(stream);
        }

        /// <summary>
        /// 打开流，用于写入
        /// </summary>
        /// <param name="baseStream">基础流</param>
        /// <returns></returns>
        public virtual Stream OpenWrite(Stream baseStream)
        {
            WriteHeader(baseStream);
            return baseStream;
        }
    }
}
