namespace System.Web.Script.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web.Resources;

    public static class Json
    {
        private readonly static JavaScriptSerializer _Serializer = null;
        private static int _recursionLimit;

        static Json()
        {
            _Serializer = new JavaScriptSerializer();
            _recursionLimit = 100;
        }

        public static T ConvertToType<T>(object obj) => 
            ((T) ObjectConverter.ConvertObjectToType(obj, typeof(T), _Serializer));

        public static object ConvertToType(object obj, Type targetType) => 
            ObjectConverter.ConvertObjectToType(obj, targetType, _Serializer);

        public static T Parse<T>(string input) =>
            ((T)JavaScriptSerializer.Deserialize(_Serializer, input, typeof(T), _recursionLimit));

        public static object Parse(string input, Type targetType) => 
            JavaScriptSerializer.Deserialize(_Serializer, input, targetType, _recursionLimit);
        
        public static object Parse(string input) => 
            JavaScriptSerializer.Deserialize(_Serializer, input, null, _recursionLimit);
        

        public static string Stringify(object obj, SerializationFormat serializationFormat = SerializationFormat.None) =>
            _Serializer.Serialize(obj, serializationFormat);
        

        public static int MaxJsonLength
        {
            get
            { return _Serializer.MaxJsonLength; }
            set
            {
                _Serializer.MaxJsonLength = value;
            }
        }

        public static int RecursionLimit
        {
            get { return _recursionLimit; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(AtlasWeb.JSON_InvalidRecursionLimit);
                }
                _Serializer.RecursionLimit = _recursionLimit = value;
            }
        }

    }
}

