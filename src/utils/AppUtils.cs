using System.Reflection;
using System.IO;

namespace KillerOfUnwantedWindows1C.src.utils
{
    class AppUtils
    {
        public static string AppDir()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}