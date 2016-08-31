using System;

namespace ImageResizer.Plugins.GradientOverlay.Extensions
{
    public static class StringExtensions
    {
        public static bool IsEnabled(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            if (input.Equals("1")) return true;
            if (input.Equals("true", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }
    }
}