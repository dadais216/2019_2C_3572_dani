using System;
using TGC.Core.Mathematica;

namespace TGC.Group.Model
{
    internal static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + @" > " + message);
        }
        public static void LogVector(TGCVector3 vec)
        {
            Log("( "+vec.X.ToString()+"   " + vec.Y.ToString() + "   " + vec.Z.ToString() + "  )");
        }
    }
}
