namespace System.Web.Script.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Resources;
    using System.Web.Util;

    internal class JavaScriptObjectDeserializer
    {
        private int _depthLimit;
        internal JavaScriptString _s;
        private JavaScriptSerializer _serializer;
        private const string DateTimePrefix = "\"\\/Date(";
        private const int DateTimePrefixLength = 8;
        private const string DateTimeSuffix = "\\/\"";
        private const int DateTimeSuffixLength = 3;

        private JavaScriptObjectDeserializer(string input, int depthLimit, JavaScriptSerializer serializer)
        {
            this._s = new JavaScriptString(input);
            this._depthLimit = depthLimit;
            this._serializer = serializer;
        }

        private void AppendCharToBuilder(char? c, StringBuilder sb)
        {
            char? nullable2 = c;
            int nullable = nullable2.GetValueOrDefault();
            if (nullable == 0x22 || nullable == 0x27 || nullable == 0x2f)
            {
                sb.Append(c.Value);
                return;
            }
            switch (nullable)
            {
                case 0x62:
                    sb.Append('\b');
                    break;
                case 0x66:
                    sb.Append('\f');
                    break;
                case 110:
                    sb.Append('\n');
                    break;
                case 0x72:
                    sb.Append('\r');
                    break;
                case 0x74:
                    sb.Append('\t');
                    break;
                case 0x75:
                    sb.Append((char)int.Parse(this._s.MoveNext(4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_BadEscape));
            }
        }

        internal static object BasicDeserialize(string input, int depthLimit, JavaScriptSerializer serializer)
        {
            JavaScriptObjectDeserializer deserializer = new JavaScriptObjectDeserializer(input, depthLimit, serializer);
            object obj2 = deserializer.DeserializeInternal(0);
            if (deserializer._s.GetNextNonEmptyChar().HasValue)
            {
                object[] args = new object[] { deserializer._s.ToString() };
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_IllegalPrimitive, args));
            }
            return obj2;
        }

        private char CheckQuoteChar(char? c)
        {
            char? nullable2 = c;
            char c1 = nullable2.GetValueOrDefault();
            if (c1 == 0x27 )
            {
                return c.Value;
            }
            if (c1!=0x22 )
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_StringNotQuoted));
            }
            return '"';
        }

        private IDictionary<string, object> DeserializeDictionary(int depth)
        {
            IDictionary<string, object> dictionary = null;
            char? nextNonEmptyChar;
            char? nullable3 = this._s.MoveNext();
            int num = 0x7b;
            if ((nullable3.GetValueOrDefault() != num) )
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_ExpectedOpenBrace));
            }
        Label_0254:
            nullable3 = nextNonEmptyChar = this._s.GetNextNonEmptyChar();
            if (nullable3.HasValue)
            {
                this._s.MovePrev();
                nullable3 = nextNonEmptyChar;
                num = 0x3a;
                if ((nullable3.GetValueOrDefault() == num) )
                {
                    throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidMemberName));
                }
                string str = null;
                nullable3 = nextNonEmptyChar;
                num = 0x7d;
                if ((nullable3.GetValueOrDefault() != num) )
                {
                    str = this.DeserializeMemberName();
                    nullable3 = this._s.GetNextNonEmptyChar();
                    num = 0x3a;
                    if ((nullable3.GetValueOrDefault() != num))
                    {
                        throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
                    }
                }
                if (dictionary == null)
                {
                    dictionary = new Dictionary<string, object>();
                    if (str == null)
                    {
                        nextNonEmptyChar = this._s.GetNextNonEmptyChar();
                        goto Label_026F;
                    }
                }
                this.ThrowIfMaxJsonDeserializerMembersExceeded(dictionary.Count);
                object obj2 = this.DeserializeInternal(depth);
                dictionary[str] = obj2;
                nextNonEmptyChar = this._s.GetNextNonEmptyChar();
                nullable3 = nextNonEmptyChar;
                num = 0x7d;
                if (!((nullable3.GetValueOrDefault() == num) ))
                {
                    nullable3 = nextNonEmptyChar;
                    num = 0x2c;
                    if ((nullable3.GetValueOrDefault() != num) )
                    {
                        throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
                    }
                    goto Label_0254;
                }
            }
        Label_026F:
            nullable3 = nextNonEmptyChar;
            num = 0x7d;
            if ((nullable3.GetValueOrDefault() != num))
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
            }
            return dictionary;
        }

        private object DeserializeInternal(int depth)
        {
            if (++depth > this._depthLimit)
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_DepthLimitExceeded));
            }
            char? nextNonEmptyChar = this._s.GetNextNonEmptyChar();
            if (!nextNonEmptyChar.HasValue)
            {
                return null;
            }
            this._s.MovePrev();
            if (this.IsNextElementDateTime())
            {
                return this.DeserializeStringIntoDateTime();
            }
            if (IsNextElementObject(nextNonEmptyChar))
            {
                IDictionary<string, object> o = this.DeserializeDictionary(depth);
                if (o.ContainsKey("__type"))
                {
                    return ObjectConverter.ConvertObjectToType(o, null, this._serializer);
                }
                return o;
            }
            if (IsNextElementArray(nextNonEmptyChar))
            {
                return this.DeserializeList(depth);
            }
            if (IsNextElementString(nextNonEmptyChar))
            {
                return this.DeserializeString();
            }
            return this.DeserializePrimitiveObject();
        }

        private IList DeserializeList(int depth)
        {
            char? nextNonEmptyChar;
            IList list = new ArrayList();
            char? nullable3 = this._s.MoveNext();
            int num = 0x5b;
            if (nullable3.GetValueOrDefault()!=num)
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayStart));
            }
            bool flag = false;
        Label_013B:
            nullable3 = nextNonEmptyChar = this._s.GetNextNonEmptyChar();
            if (nullable3.HasValue)
            {
                nullable3 = nextNonEmptyChar;
                num = 0x5d;
                if (nullable3.GetValueOrDefault()!=num)
                {
                    this._s.MovePrev();
                    object obj2 = this.DeserializeInternal(depth);
                    list.Add(obj2);
                    flag = false;
                    nextNonEmptyChar = this._s.GetNextNonEmptyChar();
                    nullable3 = nextNonEmptyChar;
                    num = 0x5d;
                    if (nullable3.GetValueOrDefault()!=num)
                    {
                        flag = true;
                        num = 0x2c;
                        if ((nullable3.GetValueOrDefault() != num))
                        {
                            throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayExpectComma));
                        }
                        goto Label_013B;
                    }
                }
            }
            if (flag)
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayExtraComma));
            }
            nullable3 = nextNonEmptyChar;
            num = 0x5d;
            if ((nullable3.GetValueOrDefault() != num))
            {
                throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayEnd));
            }
            return list;
        }

        private string DeserializeMemberName()
        {
            char? nextNonEmptyChar = this._s.GetNextNonEmptyChar();
            if (!nextNonEmptyChar.HasValue)
            {
                return null;
            }
            this._s.MovePrev();
            if (IsNextElementString(nextNonEmptyChar))
            {
                return this.DeserializeString();
            }
            return this.DeserializePrimitiveToken();
        }

        private object DeserializePrimitiveObject()
        {
            double num;
            string s = this.DeserializePrimitiveToken();
            if (s.Equals("null"))
            {
                return null;
            }
            if (s.Equals("true"))
            {
                return true;
            }
            if (s.Equals("false"))
            {
                return false;
            }
            bool flag = s.IndexOf('.') >= 0;
            if (s.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) < 0)
            {
                decimal num2;
                if (!flag)
                {
                    int num3;
                    long num4;
                    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num3))
                    {
                        return num3;
                    }
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num4))
                    {
                        return num4;
                    }
                }
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out num2))
                {
                    return num2;
                }
            }
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
            {
                return num;
            }
            object[] args = new object[] { s };
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_IllegalPrimitive, args));
        }

        private string DeserializePrimitiveToken()
        {
            char? nullable2;
            StringBuilder builder = new StringBuilder();
            char? nullable = null;
        Label_0067:
            nullable2 = nullable = this._s.MoveNext();
            if (nullable2.HasValue)
            {
                if ((char.IsLetterOrDigit(nullable.Value) || (nullable.Value == '.')) || (((nullable.Value == '-') || (nullable.Value == '_')) || (nullable.Value == '+')))
                {
                    builder.Append(nullable.Value);
                }
                else
                {
                    this._s.MovePrev();
                    goto Label_007E;
                }
                goto Label_0067;
            }
        Label_007E:
            return builder.ToString();
        }

        private string DeserializeString()
        {
            StringBuilder sb = new StringBuilder();
            bool flag = false;
            char? c = this._s.MoveNext();
            char ch = this.CheckQuoteChar(c);
            while (true)
            {
                char? nullable3 = c = this._s.MoveNext();
                if (!nullable3.HasValue)
                {
                    throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_UnterminatedString));
                }
                int num = 0x5c;
                if (nullable3.Value == num)
                {
                    if (flag)
                    {
                        sb.Append('\\');
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                else if (flag)
                {
                    this.AppendCharToBuilder(c, sb);
                    flag = false;
                }
                else
                {
                    if (nullable3.Value == ch)
                    {
                        return Utf16StringValidator.ValidateString(sb.ToString(), true);
                    }
                    sb.Append(c.Value);
                }
            }
        }

        private object DeserializeStringIntoDateTime()
        {
            long num2;
            int index = this._s.IndexOf("\\/\"");
            Match match = Regex.Match(this._s.Substring(index + 3), "^\"\\\\/Date\\((?<ticks>-?[0-9]+)(?:[a-zA-Z]|(?:\\+|-)[0-9]{4})?\\)\\\\/\"");
            if (long.TryParse(match.Groups["ticks"].Value, out num2))
            {
                this._s.MoveNext(match.Length);
                return new DateTime((num2 * 0x2710L) + JavaScriptSerializer.DatetimeMinTimeTicks, DateTimeKind.Utc);
            }
            return this.DeserializeString();
        }

        private static bool IsNextElementArray(char? c)
        {
            char? nullable2 = c;
            int? nullable = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
            int num = 0x5b;
            if (nullable.GetValueOrDefault() != num)
            {
                return false;
            }
            return nullable.HasValue;
        }

        private bool IsNextElementDateTime()
        {
            string a = this._s.MoveNext(8);
            if (a != null)
            {
                this._s.MovePrev(8);
                return string.Equals(a, "\"\\/Date(", StringComparison.Ordinal);
            }
            return false;
        }

        private static bool IsNextElementObject(char? c)
        {
            char? nullable2 = c;
            int? nullable = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
            int num = 0x7b;
            if (nullable.GetValueOrDefault() != num)
            {
                return false;
            }
            return nullable.HasValue;
        }

        private static bool IsNextElementString(char? c)
        {
            char? nullable2 = c;
            int? nullable = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
            int num = 0x22;
            if ((nullable.GetValueOrDefault() == num) ? nullable.HasValue : false)
            {
                return true;
            }
            nullable2 = c;
            nullable = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
            num = 0x27;
            if (nullable.GetValueOrDefault() != num)
            {
                return false;
            }
            return nullable.HasValue;
        }

        private void ThrowIfMaxJsonDeserializerMembersExceeded(int count)
        {
            if (count >= 0x7fffffff)
            {
                object[] args = new object[] { 0x7fffffff };
                throw new InvalidOperationException(("CollectionCountExceeded_JavaScriptObjectDeserializer"));
            }
        }
    }
}

