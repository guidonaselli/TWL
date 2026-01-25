using System.Resources;
using System.Reflection;

namespace TWL.Client.Presentation.Services
{
    public static class Loc
    {
        private static readonly ResourceManager _resourceManager =
            new ResourceManager("TWL.Client.Resources.Strings", Assembly.GetExecutingAssembly());

        public static string T(string key)
        {
            return _resourceManager.GetString(key) ?? $"[[{key}]]";
        }

        public static string TF(string key, params object[] args)
        {
            var format = T(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}
