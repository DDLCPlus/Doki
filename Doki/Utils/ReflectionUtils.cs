using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Utils
{
    public static class ReflectionUtils
    {
        public static object GetPrivateField(this object what, string oh)
        {
            return what.GetType().GetField(oh, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(what);
        }

        public static void SetPrivateField(this object what, string oh, object wow)
        {
            what.GetType().GetField(oh, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(what, wow);
        }

        public static object InvokePrivateMethod(this object what, string wow, object[] parameters)
        {
            return what.GetType().GetMethod(wow, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(what, parameters);
        }

        public static object GetStaticField(this Type what, string field)
        {
            return what.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        public static void SetStaticField(this Type what, string field, object value)
        {
            what.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        }
    }
}
