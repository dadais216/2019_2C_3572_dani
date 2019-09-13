using System;

namespace TGC.Group.Model
{
    internal static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + @" > " + message);
        }
    }
}
