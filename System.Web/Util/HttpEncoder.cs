namespace System.Web.Util
{
    using System.Globalization;
    using System.Text;

    internal class HttpEncoder
    {
        private static readonly HttpEncoder _defaultEncoder = new HttpEncoder();
        private static readonly string[] _headerEncodingTable = new string[] {
            "%00", "%01", "%02", "%03", "%04", "%05", "%06", "%07", "%08", "%09", "%0a", "%0b", "%0c", "%0d", "%0e", "%0f",
            "%10", "%11", "%12", "%13", "%14", "%15", "%16", "%17", "%18", "%19", "%1a", "%1b", "%1c", "%1d", "%1e", "%1f"
        };
        private readonly bool _isDefaultEncoder;

        public HttpEncoder()
        {
            this._isDefaultEncoder = base.GetType() == typeof(HttpEncoder);
        }

        private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c)
        {
            builder.Append(@"\u");
            builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }

        private bool CharRequiresJavaScriptEncoding(char c)
        {
            if (c >= ' ' && c != '"' && c != '\\' && c != '\'' && (c != '&' || !this.JavaScriptEncodeAmpersand) && c != '\x0085' && c != '\u2028' && c != '\u2029')
            {
                return false;
            }
            return true;
        }


        private static bool IsNonAsciiByte(byte b)
        {
            if (b < 0x7f)
            {
                return (b < 0x20);
            }
            return true;
        }

        protected internal virtual string JavaScriptStringEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            StringBuilder builder = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (this.CharRequiresJavaScriptEncoding(c))
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length + 5);
                    }
                    if (count > 0)
                    {
                        builder.Append(value, startIndex, count);
                    }
                    startIndex = i + 1;
                    count = 0;
                }
                switch (c)
                {
                    case '\b':
                        {
                            builder.Append(@"\b");
                            continue;
                        }
                    case '\t':
                        {
                            builder.Append(@"\t");
                            continue;
                        }
                    case '\n':
                        {
                            builder.Append(@"\n");
                            continue;
                        }
                    case '\f':
                        {
                            builder.Append(@"\f");
                            continue;
                        }
                    case '\r':
                        {
                            builder.Append(@"\r");
                            continue;
                        }
                    case '"':
                        {
                            builder.Append("\\\"");
                            continue;
                        }
                    case '\'':
                        {
                            builder.Append("'");
                            continue;
                        }
                    case '\\':
                        {
                            builder.Append(@"\\");
                            continue;
                        }
                }
                if (this.CharRequiresJavaScriptEncoding(c))
                {
                    AppendCharAsUnicodeJavaScript(builder, c);
                }
                else
                {
                    count++;
                }
            }
            if (builder == null)
            {
                return value;
            }
            if (count > 0)
            {
                builder.Append(value, startIndex, count);
            }
            return builder.ToString();
        }

        public static HttpEncoder Current
        {
            get
            {
                return _defaultEncoder;
            }
        }

        public static HttpEncoder Default =>
            _defaultEncoder;


        public bool JavaScriptEncodeAmpersand { get; set; }
    }
}

