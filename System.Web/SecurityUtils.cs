namespace System.Web
{
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;

    internal static class SecurityUtils
    {
        internal static object FieldInfoGetValue(FieldInfo field, object target)
        {
            return field.GetValue(target);
        }
        internal static object MethodInfoInvoke(MethodInfo method, object target, object[] args)
        {
            return method.Invoke(target, args);
        }

    }
}

