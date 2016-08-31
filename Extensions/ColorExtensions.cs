using System;
using System.Drawing;

namespace ImageResizer.Plugins.GradientOverlay.Extensions
{
    public static class ColorExtensions
    {
        public static Color ToColorFromHtml(this string htmlColor)
        {
            var c = Color.Empty;

            if (string.IsNullOrEmpty(htmlColor)) return c;

            // #AARRGGBB or #RRGGBB or #ARGB or #RGB
            if ((htmlColor.Length == 8) || (htmlColor.Length == 6) || (htmlColor.Length == 4) || (htmlColor.Length == 3))
            {
                switch (htmlColor.Length)
                {
                    case 8:
                        c = Color.FromArgb(
                            Convert.ToInt32(htmlColor.Substring(0, 2), 16),
                            Convert.ToInt32(htmlColor.Substring(2, 2), 16),
                            Convert.ToInt32(htmlColor.Substring(4, 2), 16),
                            Convert.ToInt32(htmlColor.Substring(6, 2), 16));

                        break;

                    case 6:
                        c = Color.FromArgb(
                            Convert.ToInt32(htmlColor.Substring(0, 2), 16),
                            Convert.ToInt32(htmlColor.Substring(2, 2), 16),
                            Convert.ToInt32(htmlColor.Substring(4, 2), 16));

                        break;

                    case 4:
                    {
                        var a = char.ToString(htmlColor[0]);
                        var r = char.ToString(htmlColor[1]);
                        var g = char.ToString(htmlColor[2]);
                        var b = char.ToString(htmlColor[3]);

                        c = Color.FromArgb(Convert.ToInt32(a + a, 16),
                            Convert.ToInt32(r + r, 16),
                            Convert.ToInt32(g + g, 16),
                            Convert.ToInt32(b + b, 16));
                    }
                        break;

                    default:
                    {
                        var r = char.ToString(htmlColor[0]);
                        var g = char.ToString(htmlColor[1]);
                        var b = char.ToString(htmlColor[2]);

                        c = Color.FromArgb(Convert.ToInt32(r + r, 16),
                            Convert.ToInt32(g + g, 16),
                            Convert.ToInt32(b + b, 16));
                    }
                        break;
                }
            }
           
            return c;

        }

    }
}
