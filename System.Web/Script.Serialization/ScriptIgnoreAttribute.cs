namespace System.Web.Script.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed class ScriptIgnoreAttribute : Attribute
    {
        public bool Ignore { get; set; } = true;
    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FieldMapAttribute : Attribute
    {
        public string As { get; set; } = "";
    }
}

