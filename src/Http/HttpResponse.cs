using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;

namespace IocpSharp.Http
{
    public class HttpResponse
    {

        private int _statusCode = 200;
        private string _statusText = "OK";
        private string _httpProtocol = null;

        private NameValueCollection _headers = new NameValueCollection();

        public NameValueCollection Headers => _headers;

        public int StatusCode { 
            get => _statusCode; 
            set {
                string statusText = HttpStatus.GetStatus(value);

                if (string.IsNullOrEmpty(statusText))
                    throw new Exception($"未知状态码：{value}");

                _statusCode = value;
                _statusText = statusText;
            }
        }

        internal HttpResponse() { }

        /// <summary>
        /// 使用状态码和协议实例化一个HttpResponse类
        /// </summary>
        /// <param name="statusCode">状态码，200、400等</param>
        /// <param name="httpProtocol">协议，默认用HTTP/1.1</param>
        public HttpResponse(int statusCode, string httpProtocol = "HTTP/1.1")
        {
            StatusCode = statusCode;
            _httpProtocol = httpProtocol;
        }

        /// <summary>
        /// 添加响应头
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        public void AddHeader(string name, string value)
        {
            _headers.Add(name, value);
        }

        /// <summary>
        /// 设置响应头
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        public void SetHeader(string name, string value)
        {
            _headers.Set(name, value);
        }

        /// <summary>
        /// 删除响应头
        /// </summary>
        /// <param name="name">名称</param>
        public void RemoveHeader(string name)
        {
            _headers.Remove(name);
        }


        /// <summary>
        /// 获获取完整的响应头
        /// </summary>
        /// <returns>完整响应头</returns>
        public string GetAllResponseHeaders()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}\r\n", _httpProtocol, _statusCode, _statusText);

            foreach (string name in _headers.Keys)
            {
                string[] values = _headers.GetValues(name);
                if (values == null || values.Length == 0) continue;

                foreach (string value in values)
                {
                    if (value == null) continue;
                    sb.AppendFormat("{0}: {1}\r\n", name, value);
                }
            }
            sb.Append("\r\n");

            return sb.ToString();
        }
    }
}
