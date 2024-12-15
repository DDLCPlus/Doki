using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Extensions
{
    public static class ReflectionUtils
    {
        public static object GetPrivateField(this object what, string oh) => what.GetType().GetField(oh, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(what);
        public static void SetPrivateField(this object what, string oh, object wow) => what.GetType().GetField(oh, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(what, wow);

        public static object GetStaticField(this Type what, string field) => what.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        public static void SetStaticField(this Type what, string field, object value) => what.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);

        public static object InvokePrivateMethod(this object what, string wow, object[] parameters) => what.GetType().GetMethod(wow, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(what, parameters);
    }
}
