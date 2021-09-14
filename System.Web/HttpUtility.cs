namespace System.Web
{
    using Util;

    internal sealed class HttpUtility
    {

        public static string JavaScriptStringEncode(string value)
        {
            return JavaScriptStringEncode(value, false);
        }
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            string str = HttpEncoder.Current.JavaScriptStringEncode(value);
            if (!addDoubleQuotes)
            {
                return str;
            }
            return ("\"" + str + "\"");
        }
    }       
}

