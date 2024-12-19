using System;
using System.Reflection;

namespace Doki.Extensions
{
    public static class ReflectionUtils
    {
        public static object GetPrivateField(this object obj, string field) =>
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        public static void SetPrivateField(this object obj, string field, object value) =>
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);

        public static object GetStaticField(this Type obj, string field) =>
            obj.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        public static void SetStaticField(this Type obj, string field, object value) =>
            obj.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);

        public static object InvokePrivateMethod(this object obj, string value, object[] parameters) =>
            obj.GetType().GetMethod(value, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, parameters);
    }
}
