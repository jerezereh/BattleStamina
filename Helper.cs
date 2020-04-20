using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace BattleStamina
{
    public static class Helper
    {
        internal static object Call(this object o, string methodName, params object[] args)
        {
            MethodInfo method = o.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                try
                {
                    return method.Invoke(o, args);
                }
                catch
                {
                }
            }
            return null;
        }

        internal static MethodInfo GetMethod(this object o, string methodName, params object[] args)
        {
            MethodInfo method = o.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return method;
        }

        internal static object GetField(this object o, string fieldName)
        {
            FieldInfo field = o.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                try
                {
                    return field.GetValue(o);
                }
                catch
                {
                }
            }
            return null;
        }

        public static T Deserialize<T>(string filename)
        {
            TextReader textReader = null;
            T obj = default;
            try
            {
                textReader = new StreamReader(filename);
                obj = (T)new XmlSerializer(typeof(T)).Deserialize(textReader);
            }

            finally
            {
                textReader?.Close();
            }
            return obj;
        }
    }
}