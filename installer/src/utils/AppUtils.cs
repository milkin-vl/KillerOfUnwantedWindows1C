using System.Reflection;
using System.IO;

namespace installer.src.utils
{
    class AppUtils
    {
        public static string AppDir()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
