using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer.Configuration;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.GradientOverlay.Extensions;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.GradientOverlay
{
    public class GradientOverlayPlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[]
            {
                Parameters.Enabled,
                Parameters.GradientStart,
                Parameters.GradientEnd,
                Parameters.Mode,
                Parameters.ColorStart,
                Parameters.ColorEnd,
                Parameters.ColorFill,
                Parameters.GradientClamp,
                Parameters.Mirror
            };
        }

        protected override RequestedAction PostRenderImage(ImageState state)
        {
            if (state.destBitmap == null)
                return RequestedAction.None;

            var settings = GetSettings(state);

            if (settings == null)
                return RequestedAction.None;

            Bitmap bitmap = null;

            try
            {
                bitmap = state.destBitmap;

                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                if (settings.Radial)
                {
                    DrawRadialGradient(data, settings);
                }
                else
                {
                    DrawLinearGradient(data, settings);
                }

                bitmap.UnlockBits(data);
            }
            finally
            {
                if (bitmap != null & bitmap != state.destBitmap)
                    bitmap.Dispose();
            }

            return RequestedAction.None;
        }

        protected Settings GetSettings(ImageState state)
        {
            var gradient = state.settings[Parameters.Enabled];

            if (string.IsNullOrEmpty(gradient))
                return null;

            if (!gradient.IsEnabled())
                return null;

            var paramColorFill = state.settings[Parameters.ColorFill] ?? "0000";
            var fillColor = paramColorFill.ToColorFromHtml();

            var paramColorStart = state.settings[Parameters.ColorStart] ?? "0000";
            var paramColorEnd = state.settings[Parameters.ColorEnd] ?? (fillColor.A == 0 ? "F000" : "0000");
            
            var colorStart = paramColorStart.ToColorFromHtml();
            var colorEnd = paramColorEnd.ToColorFromHtml();

            if (fillColor.A == 0 && colorStart.A == 0 && colorEnd.A == 0)
                return null;

            var radial = state.settings[Parameters.Mode] == "r";

            var clamp = NameValueCollectionExtensions.Get(state.settings, Parameters.GradientClamp, new int?(0)) ?? 0;

            var fallbackStart = radial ? new[] { 50, 50 } : new[] { 0 };
            var fallbackEnd = radial ? new[] { 100, 100 } : new[] { 100 };

            var paramStart = NameValueCollectionExtensions.GetList(state.settings, Parameters.GradientStart, new int?(0), 1, 2) ?? fallbackStart;
            var paramEnd = NameValueCollectionExtensions.GetList(state.settings, Parameters.GradientEnd, new int?(100), 1, 2) ?? fallbackEnd;

            var paramMirrored = state.settings[Parameters.Mirror];
            var mirrored = paramMirrored.IsEnabled();

            var settings = new Settings
            {
                Start = paramStart.VectorYFromArray(),
                End = paramEnd.VectorYFromArray(),
                Clamp = clamp * 0.01,
                FirstColor = colorStart,
                SecondColor = colorEnd,
                FillColor = fillColor,
                Radial = radial,
                Mirrored = mirrored
            };

            return settings;
        }

        private const double InvertedByte = 1.0 / 255.0;

        private static unsafe void DrawLinearGradient(BitmapData bitmap, Settings settings)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = bitmap.Height;
            var w = bitmap.Width;

            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            // Declare start and end positions as relative to image size
            var fx = settings.Start.X * w;
            var fy = settings.Start.Y * h;

            var tx = settings.End.X * w;
            var ty = settings.End.Y * h;

            // A vector representing a line from start to end
            var ftx = tx - fx;
            var fty = ty - fy;

            // This variable is an optimization
            // It precalculates the dot product of the line from start to end inversely so that
            // code can run using a multiplication instead of a division
            // as you might now multiplications are way faster
            var iftd = 1.0 / (ftx * ftx + fty * fty);

            var c1 = settings.FirstColor;
            var c2 = settings.SecondColor;
            var cf = settings.FillColor;

            var clamp = settings.Clamp;
            var mirrored = settings.Mirrored;

            double pk = -1.0, tr = 0.0, tg = 0.0, tb = 0.0, bl = 0.0, ibl = 0.0;

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    for (var x = 0; x < w; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];
                        // Note that we are not reading from, nor assigning to the alpha channel.
                        // This is to preserve cutouts, for example logotype shapes.
                        // Alpha is normally located at row[p + 3].

                        // Establish a vector from start to current position
                        // var fpx = x - fx;
                        // var fpy = y - fy;
                        // Inlined

                        // This rather unreadable part projects the vector fp
                        // to a line segment. k is 0 - 1 (percent) on that line.
                        // k could be negative or > 1, that means that
                        // the current point is outside of the bounds of ft
                        var k = ((x - fx) * ftx + (y - fy) * fty) * iftd;
                        
                        if (clamp > 0)
                        {
                            k += -clamp + (k * clamp);
                        }

                        // If colors are opaque, no need for mixing.
                        // This assigns the colors directly
                        // Also clamp the values of k to the range 0 - 1
                        if (mirrored)
                        {
                            k = Math.Abs(k);
                            k %= 2;
                            if (k > 1) k = 1 - (k - 1);
                        }
                        else
                        {
                            if (k <= 0)
                            {
                                if (c1.A == 255)
                                {
                                    row[p] = c1.B;
                                    row[p + 1] = c1.G;
                                    row[p + 2] = c1.R;
                                    continue;
                                }

                                k = 0.0;
                            }
                            else if (k >= 1)
                            {
                                if (c2.A == 255)
                                {
                                    row[p] = c2.B;
                                    row[p + 1] = c2.G;
                                    row[p + 2] = c2.R;
                                    continue;
                                }

                                k = 1.0;
                            }
                        }

                        var ik = 1.0 - k;

                        // Skip calculation if previous values are unchanged
                        // Optimization of exactly 90 degree linear gradients
                        if (k != pk)
                        {
                            // Mix colors based on k-value, linearly
                            var ta = c2.A * k + c1.A * ik;
                            tr = c2.R * k + c1.R * ik;
                            tg = c2.G * k + c1.G * ik;
                            tb = c2.B * k + c1.B * ik;

                            bl = ta * InvertedByte;
                            ibl = 1.0 - bl;
                        }

                        // Blend the colors back with original colors from image
                        tb = b * ibl + tb * bl;
                        tg = g * ibl + tg * bl;
                        tr = r * ibl + tr * bl;

                        if (cf.A > 0)
                        {
                            bl = cf.A * InvertedByte;
                            ibl = 1.0 - bl;

                            tb = tb * ibl + cf.B * bl;
                            tg = tg * ibl + cf.G * bl;
                            tr = tr * ibl + cf.R * bl;
                        }

                        row[p] = (byte)tb;    //Blue  0-255
                        row[p + 1] = (byte)tg;    //Green 0-255
                        row[p + 2] = (byte)tr;    //Red   0-255
                    }
                }
            }
        }

        private static unsafe void DrawRadialGradient(BitmapData bitmap, Settings settings)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = bitmap.Height;
            var w = bitmap.Width;

            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            // Declare start and end positions as relative to image size
            var fx = settings.Start.X * w;
            var fy = settings.Start.Y * h;

            var tx = settings.End.X * w;
            var ty = settings.End.Y * h;

            // This variable is an optimization
            // It precalculates the distance of the endpoint inversely so that
            // code can run using a multiplication instead of a division
            // as you might now multiplications are way faster
            var ir = 1.0 / Math.Sqrt((fx - tx) * (fx - tx) + (fy - ty) * (fy - ty));

            var c1 = settings.FirstColor;
            var c2 = settings.SecondColor;
            var cf = settings.FillColor;

            var mirrored = settings.Mirrored;
            var clamp = settings.Clamp;

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    for (var x = 0; x < w; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];

                        if (cf.A == 255)
                        {
                            row[p] = cf.B;
                            row[p + 1] = cf.G;
                            row[p + 2] = cf.R;
                            continue;
                        }

                        // Establish a vector from origin to current position
                        var fpx = x - fx;
                        var fpy = y - fy;

                        // This gets a value k 0 - 1 based on the distance from origin
                        // Becomes a circular form by design
                        var k = Math.Sqrt(fpx * fpx + fpy * fpy) * ir;

                        
                        if (clamp > 0.0)
                            k += -clamp + (k * clamp);

                        // If colors are opaque, no need for mixing.
                        // This assigns the colors directly
                        if (mirrored)
                        {
                            k = Math.Abs(k);
                            k %= 2;
                            if (k > 1) k = 1 - (k - 1);
                        }
                        else
                        {
                            if (k <= 0.0)
                            {
                                if (c1.A == 255)
                                {
                                    row[p] = c1.B;
                                    row[p + 1] = c1.G;
                                    row[p + 2] = c1.R;
                                    continue;
                                }

                                k = 0.0;
                            }
                            else if (k >= 1.0)
                            {
                                if (c2.A == 255)
                                {
                                    row[p] = c2.B;
                                    row[p + 1] = c2.G;
                                    row[p + 2] = c2.R;
                                    continue;
                                }

                                k = 1.0;
                            }
                        }

                        var ik = 1.0 - k;

                        // Mix colors based on k-value, linearly
                        var ta = c2.A * k + c1.A * ik;
                        var tr = c2.R * k + c1.R * ik;
                        var tg = c2.G * k + c1.G * ik;
                        var tb = c2.B * k + c1.B * ik;

                        var bl = ta * InvertedByte;
                        var ibl = 1.0 - bl;

                        // Blend the colors back with original colors from image
                        tb = b * ibl + tb * bl;
                        tg = g * ibl + tg * bl;
                        tr = r * ibl + tr * bl;

                        if (cf.A > 0)
                        {
                            bl = cf.A * InvertedByte;
                            ibl = 1.0 - bl;

                            tb = tb * ibl + cf.B * bl;
                            tg = tg * ibl + cf.G * bl;
                            tr = tr * ibl + cf.R * bl;
                        }

                        row[p] = (byte)tb;    //Blue  0-255
                        row[p + 1] = (byte)tg;    //Green 0-255
                        row[p + 2] = (byte)tr;    //Red   0-255
                    }
                }
            }
        }
       
    }
}
