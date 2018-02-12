using System.Text.RegularExpressions;

namespace UnityScript2CSharp.Extensions
{
    public static class StringExtensions
    {
        public static bool IsSwitchLabel(this string candidate)
        {
            return Regex.IsMatch(candidate, @":?\$switch\$\d+");
        }
    }
}
