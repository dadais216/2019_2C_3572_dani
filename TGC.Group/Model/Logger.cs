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
        public static void Log(TGCVector3 vec)
        {
            Log("( "+vec.X.ToString()+"   " + vec.Y.ToString() + "   " + vec.Z.ToString() + "  )");
        }
        public static void Log(TGCMatrix vec)
        {
            Log("( " + vec.M11.ToString() + "   " + vec.M12.ToString() + "   " + vec.M13.ToString() + "   " + vec.M14.ToString() + "  )");
            Log("( " + vec.M21.ToString() + "   " + vec.M22.ToString() + "   " + vec.M23.ToString() + "   " + vec.M24.ToString() + "  )");
            Log("( " + vec.M31.ToString() + "   " + vec.M32.ToString() + "   " + vec.M33.ToString() + "   " + vec.M34.ToString() + "  )");
            Log("( " + vec.M41.ToString() + "   " + vec.M42.ToString() + "   " + vec.M43.ToString() + "   " + vec.M44.ToString() + "  )");
        }
        public static void Log(float f)
        {
            Log(f.ToString());
        }
    }
}
