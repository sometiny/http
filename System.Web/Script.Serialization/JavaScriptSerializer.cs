namespace System.Web.Script.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Web;
    using System.Web.Resources;
    using System.Collections.Specialized;

    public enum SerializationFormat
    {
        None = 0,
        Wellformed = 1,
        TimeStamp = 2,
        FormatedTimeString = 4
    }
    public class JavaScriptSerializer
    {
        private int _maxJsonLength;
        private int _recursionLimit;
        //private DateFormat _DateFormat = DateFormat.Date;
        internal static readonly long DatetimeMinTimeTicks;
        internal static readonly DateTime DatetimeMinTime;
        internal const int DefaultMaxJsonLength = int.MaxValue;
        internal const int DefaultRecursionLimit = 100;

        static JavaScriptSerializer()
        {
            DatetimeMinTime = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DatetimeMinTimeTicks = DatetimeMinTime.Ticks;
        }

        public JavaScriptSerializer()
        {
            RecursionLimit = 100;
            MaxJsonLength = int.MaxValue;
        }

        private string GetFieldMapAttribute(MemberInfo memberInfo)
        {
            if (!memberInfo.IsDefined(typeof(FieldMapAttribute), true))
            {
                return memberInfo.Name;
            }
            FieldMapAttribute attribute = (FieldMapAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(FieldMapAttribute), true);
            if (attribute == null || string.IsNullOrEmpty(attribute.As))
                return memberInfo.Name;
            return attribute.As; 
        }
        private bool CheckScriptIgnoreAttribute(MemberInfo memberInfo)
        {
            if (memberInfo.IsDefined(typeof(ScriptIgnoreAttribute), true))
            {
                return true;
            }
            ScriptIgnoreAttribute attribute = (ScriptIgnoreAttribute) Attribute.GetCustomAttribute(memberInfo, typeof(ScriptIgnoreAttribute), true);
            return ((attribute != null) && attribute.Ignore);
        }

        public T ConvertToType<T>(object obj) => 
            ((T) ObjectConverter.ConvertObjectToType(obj, typeof(T), this));

        public object ConvertToType(object obj, Type targetType) => 
            ObjectConverter.ConvertObjectToType(obj, targetType, this);

        public T Deserialize<T>(string input) => 
            ((T) Deserialize(this, input, typeof(T), RecursionLimit));

        public object Deserialize(string input, Type targetType) => 
            Deserialize(this, input, targetType, RecursionLimit);

        internal static object Deserialize(JavaScriptSerializer serializer, string input, Type type, int depthLimit)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (input.Length > serializer.MaxJsonLength)
            {
                throw new ArgumentException(AtlasWeb.JSON_MaxJsonLengthExceeded, "input");
            }
            return ObjectConverter.ConvertObjectToType(JavaScriptObjectDeserializer.BasicDeserialize(input, depthLimit, serializer), type, serializer);
        }

        public object DeserializeObject(string input) => 
            Deserialize(this, input, null, RecursionLimit);

        public string Serialize(object obj) => 
            Serialize(obj, SerializationFormat.None);

        public void Serialize(object obj, StringBuilder output)
        {
            Serialize(obj, output, SerializationFormat.None);
        }

        internal string Serialize(object obj, SerializationFormat serializationFormat)
        {
            StringBuilder output = new StringBuilder();
            Serialize(obj, output, serializationFormat);
            return output.ToString();
        }

        internal void Serialize(object obj, StringBuilder output, SerializationFormat serializationFormat)
        {
            SerializeValue(obj, output, 0, null, serializationFormat, null);
            if (output.Length > MaxJsonLength)
            {
                throw new InvalidOperationException(AtlasWeb.JSON_MaxJsonLengthExceeded);
            }
        }

        private static void SerializeBoolean(bool o, StringBuilder sb)
        {
            if (o)
            {
                sb.Append("true");
            }
            else
            {
                sb.Append("false");
            }
        }

        private void SerializeCustomObject(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            bool flag = true;
            Type type = o.GetType();

            sb.Append('{');
            foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (CheckScriptIgnoreAttribute(info)) continue;
                if (!flag)
                {
                    Append(sb, ',', serializationFormat);
                }
                Wellformed(serializationFormat, depth, sb);
                SerializeString(GetFieldMapAttribute(info), sb);
                Append(sb, ':', serializationFormat);
                SerializeValue(SecurityUtils.FieldInfoGetValue(info, o), sb, depth, objectsInUse, serializationFormat, info);
                flag = false;
            }
            foreach (PropertyInfo info2 in type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
            {
                if (CheckScriptIgnoreAttribute(info2)) continue;
                MethodInfo getMethod = info2.GetGetMethod();
                if ((getMethod != null) && (getMethod.GetParameters().Length == 0))
                {
                    if (!flag)
                    {
                        Append(sb, ',', serializationFormat);
                    }
                    Wellformed(serializationFormat, depth, sb);
                    SerializeString(GetFieldMapAttribute(info2), sb);
                    Append(sb, ':', serializationFormat);
                    SerializeValue(SecurityUtils.MethodInfoInvoke(getMethod, o, null), sb, depth, objectsInUse, serializationFormat, info2);
                    flag = false;
                }
            }
            Wellformed(serializationFormat, depth - 1, sb, true);
            sb.Append('}');
        }

        private static void Append(StringBuilder sb, char chr, SerializationFormat serializationFormat) {
            sb.Append(chr);

            if ((serializationFormat & SerializationFormat.Wellformed) == SerializationFormat.Wellformed)
            {
                sb.Append(' ');
            }
        }

        private static void SerializeDateTime(DateTime datetime, StringBuilder sb, SerializationFormat serializationFormat)
        {
            if((serializationFormat & SerializationFormat.FormatedTimeString) > 0)
            {
                sb.Append("\"" + datetime.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                return;
            }
            if ((serializationFormat & SerializationFormat.TimeStamp) > 0)
            {
                sb.Append((int)(datetime.ToUniversalTime() - DatetimeMinTime).TotalSeconds);
                return;
            }
            sb.Append("\"\\/Date(");
            sb.Append((datetime.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 0x2710L);
            sb.Append(")\\/\"");
        }

        private void SerializeDictionary(IDictionary o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            sb.Append('{');
            bool flag = true;
            foreach (DictionaryEntry entry in o)
            {
                string key = entry.Key as string;
                if (key == null)
                {
                    object[] args = new object[] { o.GetType().FullName };
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_DictionaryTypeNotSupported, args));
                }
                if (!flag)
                {
                    Append(sb, ',', serializationFormat);
                }
                SerializeDictionaryKeyValue(key, entry.Value, sb, depth, objectsInUse, serializationFormat);
                flag = false;
            }
            Wellformed(serializationFormat, depth - 1, sb, true);
            sb.Append('}');
        }

        private void SerializeDictionaryKeyValue(string key, object value, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            Wellformed(serializationFormat, depth, sb);
            SerializeString(key, sb);
            Append(sb, ':', serializationFormat);
            SerializeValue(value, sb, depth, objectsInUse, serializationFormat, null);
        }

        private void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat)
        {
            sb.Append('[');
            bool flag = true;
            foreach (object obj2 in enumerable)
            {
                if (!flag)
                {
                    Append(sb, ',', serializationFormat);
                }
                SerializeValue(obj2, sb, depth - 1, objectsInUse, serializationFormat, null);
                flag = false;
            }
            sb.Append(']');
        }
        private void SerializeNameValued(NameValueCollection enumerable, 
            StringBuilder sb, 
            int depth, 
            Hashtable objectsInUse, 
            SerializationFormat serializationFormat)
        {
            sb.Append('{');
            bool flag = true;
            var keys = enumerable.Keys;
            foreach (string key in keys)
            {
                if (!flag)
                {
                    Append(sb, ',', serializationFormat);
                }
                var values = enumerable.GetValues(key);
                if(values == null || values.Length == 0)
                {
                    SerializeDictionaryKeyValue(key, null, sb, depth, objectsInUse, serializationFormat);
                }
                else if(values.Length == 1)
                {
                    SerializeDictionaryKeyValue(key, values[0], sb, depth, objectsInUse, serializationFormat);
                }
                else
                {
                    SerializeDictionaryKeyValue(key, values, sb, depth, objectsInUse, serializationFormat);
                }
                flag = false;
            }
            Wellformed(serializationFormat, depth - 1, sb, true);
            sb.Append('}');
        }
        

        private static void SerializeGuid(Guid guid, StringBuilder sb)
        {
            sb.Append("\"").Append(guid.ToString()).Append("\"");
        }

        internal static string SerializeInternal(object o) => 
            new JavaScriptSerializer().Serialize(o);

        private static void SerializeString(string input, StringBuilder sb)
        {
            sb.Append('"');
            sb.Append(HttpUtility.JavaScriptStringEncode(input));
            sb.Append('"');
        }

        private static void SerializeUri(Uri uri, StringBuilder sb)
        {
            sb.Append("\"").Append(uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped)).Append("\"");
        }

        private void SerializeValue(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, MemberInfo currentMember = null)
        {
            if (++depth > _recursionLimit)
            {
                throw new ArgumentException(AtlasWeb.JSON_DepthLimitExceeded);
            }
            SerializeValueInternal(o, sb, depth, objectsInUse, serializationFormat, currentMember);
        }
        private bool IsWellformed(SerializationFormat serializationFormat)
        {
            return (serializationFormat & SerializationFormat.Wellformed) == SerializationFormat.Wellformed;
        }
        private void Wellformed(SerializationFormat serializationFormat, int depth, StringBuilder sb, bool isEnd = false)
        {
            if ((serializationFormat & SerializationFormat.Wellformed) != SerializationFormat.Wellformed) return;
            if (depth <= 0)
            {
                if (isEnd)
                {
                    sb.Append("\r\n");
                }
                return;
            }
            sb.Append("\r\n".PadRight((depth*4) + 2, ' '));
        }
        private string GetWellformed(SerializationFormat serializationFormat, int depth)
        {
            if ((serializationFormat & SerializationFormat.Wellformed) != SerializationFormat.Wellformed) return "";
            return "\r\n".PadRight((depth * 4) + 2, ' ');
        }
        private void SerializeValueInternal(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, MemberInfo currentMember)
        {
            if ((o == null) || DBNull.Value.Equals(o))
            {
                sb.Append("null");
                return;
            }
            string input = o as string;
            if (input != null)
            {
                SerializeString(input, sb);
                return;
            }
            if (o is char)
            {
                if (((char)o) == '\0')
                {
                    sb.Append("null");
                }
                else
                {
                    SerializeString(o.ToString(), sb);
                }
                return;
            }
            if (o is bool)
            {
                SerializeBoolean((bool)o, sb);
                return;
            }
            if (o is DateTime)
            {
                SerializeDateTime((DateTime)o, sb, serializationFormat);
                return;
            }
            if (o is System.Net.IPAddress )
            {
                SerializeString(o.ToString(), sb);
                return;
            }
            if (o is DateTimeOffset)
            {
                DateTimeOffset offset = (DateTimeOffset)o;
                SerializeDateTime(offset.UtcDateTime, sb, serializationFormat);
                return;
            }
            if (o is Guid)
            {
                SerializeGuid((Guid)o, sb);
                return;
            }

            Uri uri = o as Uri;
            if (uri != null)
            {
                SerializeUri(uri, sb);
                return;
            }
            if (o is double)
            {
                sb.Append(((double)o).ToString("r", CultureInfo.InvariantCulture));
                return;
            }
            if (o is float)
            {
                sb.Append(((float)o).ToString("r", CultureInfo.InvariantCulture));
                return;
            }
            if (o.GetType().IsPrimitive || (o is decimal))
            {
                IConvertible convertible = o as IConvertible;
                if (convertible != null)
                {
                    sb.Append(convertible.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(o.ToString());
                }
                return;
            }

            Type enumType = o.GetType();
            if (enumType.IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(enumType);
                if ((underlyingType == typeof(long)) || (underlyingType == typeof(ulong)))
                {
                    throw new InvalidOperationException((currentMember != null) ? (string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CannotSerializeMemberGeneric, new object[] { currentMember.Name, currentMember.ReflectedType.FullName }) + " " + AtlasWeb.JSON_InvalidEnumType) : AtlasWeb.JSON_InvalidEnumType);
                }
                sb.Append(((Enum)o).ToString("D"));
                return;
            }

            try
            {
                if (objectsInUse == null)
                {
                    objectsInUse = new Hashtable(new ReferenceComparer());
                }
                else if (objectsInUse.ContainsKey(o))
                {
                    object[] args = new object[] { enumType.FullName };
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CircularReference, args));
                }
                objectsInUse.Add(o, null);
                IDictionary dictionary = o as IDictionary;
                if (dictionary != null)
                {
                    SerializeDictionary(dictionary, sb, depth, objectsInUse, serializationFormat);
                    return;
                }
                var namevalued = o as NameValueCollection ;
                if(namevalued != null)
                {
                    SerializeNameValued(namevalued, sb, depth, objectsInUse, serializationFormat);
                    return;
                }
                IEnumerable enumerable = o as IEnumerable;
                if (enumerable != null)
                {
                    SerializeEnumerable(enumerable, sb, depth, objectsInUse, serializationFormat);
                    return;
                }
                SerializeCustomObject(o, sb, depth, objectsInUse, serializationFormat);
            }
            finally
            {
                if (objectsInUse != null)
                {
                    objectsInUse.Remove(o);
                }
            }
        }

        public int MaxJsonLength
        {
            get
            { return _maxJsonLength; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(AtlasWeb.JSON_InvalidMaxJsonLength);
                }
                _maxJsonLength = value;
            }
        }

        public int RecursionLimit
        {
            get { return _recursionLimit; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(AtlasWeb.JSON_InvalidRecursionLimit);
                }
                _recursionLimit = value;
            }
        }

        private class ReferenceComparer : IEqualityComparer
        {
            bool IEqualityComparer.Equals(object x, object y) => 
                (x == y);

            int IEqualityComparer.GetHashCode(object obj) => 
                RuntimeHelpers.GetHashCode(obj);
        }
    }
}

