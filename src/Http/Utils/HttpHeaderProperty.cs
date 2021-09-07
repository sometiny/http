using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IocpSharp.Http.Utils
{
    /// <summary>
    /// 解析HeaderLine为HttpHeaderProperty
    /// 主要用于ContentType和上传中Content-Disposition的解析
    /// </summary>
    public class HttpHeaderProperty
    {
        private string _value = null;
        private NameValueCollection _properties = null;

        public string Value => _value;

        public string this[string name]
        {
            get
            {
                if (_properties == null) return null;
                return _properties[name];
            }
        }

        public static HttpHeaderProperty Parse(string value)
        {
            int idx = value.IndexOf(';');
            if (idx == 0)
            {
                return new HttpHeaderProperty() { _value = value.Trim() };
            }

            var headerProperty = new HttpHeaderProperty() { _value = value.Substring(0, idx).Trim() };

            string propertiesString = value.Substring(idx + 1).Trim();
            if (propertiesString == string.Empty) return headerProperty;

            NameValueCollection properties = new NameValueCollection();

            //做个简单匹配
            MatchCollection matches = Regex.Matches(propertiesString, @"(\w+)=(.+?)(;|$)");
            foreach(Match match in matches)
            {
                properties.Add(match.Groups[1].Value, match.Groups[2].Value.Trim('"'));
            }

            headerProperty._properties = properties;

            return headerProperty;

        }
    }
}