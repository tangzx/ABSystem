using System;
using System.Collections.Generic;

namespace Tangzx.ABSystem
{
    public class PluginHelper
    {
        public static void ProcessPlugin<T>(Action<T> action)
        {
            Type et = typeof(PluginHelper);
            Type[] list = et.Assembly.GetTypes();
            for (int i = 0; i < list.Length; i++)
            {
                Type t = list[i];
                if (!t.IsAbstract && typeof(T).IsAssignableFrom(t))
                {
                    T n = (T)Activator.CreateInstance(t);
                    action(n);
                }
            }
        }

        public static List<T> GetPlugins<T>()
        {
            var list = new List<T>();
            ProcessPlugin<T>(t => list.Add(t));
            return list;
        }
    }
}
