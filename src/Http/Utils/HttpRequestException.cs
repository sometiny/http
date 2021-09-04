using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http
{
    public enum HttpRequestError
    {
        None,
        NotWellFormed,
        LineLengthExceedsLimit,
        NoneUrl
    }
    public class HttpRequestException : Exception
    {
        private HttpRequestError _error = HttpRequestError.None;
        public HttpRequestException(HttpRequestError error) : base()
        {
            _error = error;
        }
        public HttpRequestException(HttpRequestError error, string message) : base(message)
        {
            _error = error;
        }
        public HttpRequestException(HttpRequestError error, string message, Exception innerException) : base(message, innerException)
        {
            _error = error;
        }
        public override string Message => _error.ToString() + " => "+ base.Message;
    }
}
