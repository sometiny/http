using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http
{
    public class HttpStatus
    {
        private static readonly string[][] s_HTTPStatusDescriptions;
        private static Dictionary<int, string> m_Cached = new Dictionary<int, string>();
        static HttpStatus()
        {
            s_HTTPStatusDescriptions = new string[6][];

            s_HTTPStatusDescriptions[1] = new string[] {
                "Continue", "Switching Protocols", "Processing"
            };

            s_HTTPStatusDescriptions[2] = new string[] {
                "OK", "Created", "Accepted", "Non-Authoritative Information", "No Content", "Reset Content", "Partial Content", "Multi-Status"
            };

            s_HTTPStatusDescriptions[3] = new string[] {
                "Multiple Choices", "Moved Permanently", "Found", "See Other", "Not Modified", "Use Proxy", string.Empty, "Temporary Redirect"
            };

            s_HTTPStatusDescriptions[4] = new string[] {
                "Bad Request", "Unauthorized", "Payment Required", "Forbidden", "Not Found", "Method Not Allowed", "Not Acceptable",
                "Proxy Authentication Required", "Request Timeout", "Conflict", "Gone", "Length Required", "Precondition Failed",
                "Request Entity Too Large", "Request-Uri Too Long", "Unsupported Media Type",
                "Requested Range Not Satisfiable", "Expectation Failed", string.Empty, string.Empty, string.Empty, string.Empty,
                "Unprocessable Entity", "Locked", "Failed Dependency"
            };

            s_HTTPStatusDescriptions[5] = new string[] { "Internal Server Error", "Not Implemented", "Bad Gateway", "Service Unavailable",
                "Gateway Timeout", "Http Version Not Supported", string.Empty, "Insufficient Storage"
            };

        }

        public static string GetStatus(int code)
        {
            if (m_Cached.ContainsKey(code)) return m_Cached[code];

            int main = code / 100;
            int sub = code % 100;
            if (main <= 0 || main > 5)
            {
                m_Cached[code] = "";
                return "";
            }

            string[] texts = s_HTTPStatusDescriptions[main];
            if (sub < 0 || sub >= texts.Length)
            {
                m_Cached[code] = "";
                return "";
            }
            m_Cached[code] = texts[sub];
            return texts[sub];
        }
    }
}
