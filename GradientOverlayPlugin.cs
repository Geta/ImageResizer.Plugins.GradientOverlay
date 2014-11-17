using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer.Resizing;
using ImageResizer.Configuration;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.GradientOverlay.Primitives;
using ImageResizer.Plugins.GradientOverlay.Extensions;

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
                "gradient",
                "gstart",
                "gend",
                "gmode",
                "gcstart",
                "gcend",
                "gclamp"
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
            var gradient = state.settings["gradient"];

            if (string.IsNullOrEmpty(gradient))
                return null;

            if (gradient != "1" && gradient.ToLower() != "true")
                return null;

            var paramColorStart = state.settings["gcstart"] ?? "0000";
            var paramColorEnd = state.settings["gcend"] ?? "F000";

            var colorStart = paramColorStart.ToColorFromHtml();
            var colorEnd = paramColorEnd.ToColorFromHtml();

            if (colorStart == Color.Transparent && colorEnd == Color.Transparent)
                return null;

            var radial = state.settings["gmode"] == "r";

            var clamp = NameValueCollectionExtensions.Get(state.settings, "gclamp", new int?(0)) ?? 0;

            var fallbackStart = radial ? new[] { 50, 50 } : new[] { 0 };
            var fallbackEnd = radial ? new[] { 100, 100 } : new[] { 100 };

            var paramStart = NameValueCollectionExtensions.GetList(state.settings, "gstart", new int?(0), 1, 2) ?? fallbackStart;
            var paramEnd = NameValueCollectionExtensions.GetList(state.settings, "gend", new int?(100), 1, 2) ?? fallbackEnd;

            var settings = new Settings()
            {
                Start = paramStart.VectorYFromArray(),
                End = paramEnd.VectorYFromArray(),
                Clamp = clamp * 0.01,
                FirstColor = colorStart,
                SecondColor = colorEnd,
                Radial = radial,
            };

            return settings;
        }

        private const int ByteSize = 4;
        private const double InvertedByte = 1.0 / 255.0;

        private unsafe void DrawLinearGradient(BitmapData bitmap, Settings settings)
        {
            // Declare start and end positions as relative to image size
            var from = new Vector(settings.Start.X * bitmap.Width, settings.Start.Y * bitmap.Height);
            var to = new Vector(settings.End.X * bitmap.Width, settings.End.Y * bitmap.Height);

            // A vector representing a line from start to end
            var ft = new Vector(to.X - from.X, to.Y - from.Y);

            // This variable is an optimization
            // It precalculates the dot product of the line from start to end inversely so that
            // code can run using a multiplication instead of a division
            // as you might now multiplications are way faster
            var invftdot = 1 / (ft.X * ft.X + ft.Y * ft.Y);

            var c1 = settings.FirstColor;
            var c2 = settings.SecondColor;

            var clamp = settings.Clamp;

            for (var y = 0; y < bitmap.Height; y++)
            {
                var row = (byte*)bitmap.Scan0 + (y * bitmap.Stride);

                for (var x = 0; x < bitmap.Width; x++)
                {
                    var p = x * ByteSize;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    // Note that we are not reading from, nor assigning to the alpha channel.
                    // This is to preserve cutouts, for example logotype shapes.
                    // Alpha is normally located at row[p + 3].

                    // Establish a vector from start to current position
                    var fp = new Vector(x - from.X, y - from.Y);        

                    // This rather unreadable part projects the vector fp
                    // to a line segment. k is 0 - 1 (percent) on that line.
                    // k could be negative or > 1, that means that
                    // the current point is outside of the bounds of ft
                    var k = (fp.X * ft.X + fp.Y * ft.Y) * invftdot;

                    if (clamp > 0)
                        k += -clamp + (k * clamp);

                    // If colors are opaque, no need for mixing.
                    // This assigns the colors directly
                    // Also clamp the values of k to the range 0 - 1
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

                    // Mix colors based on k-value, linearly
                    var ta = c2.A * k + c1.A * (1.0 - k);
                    var tr = c2.R * k + c1.R * (1.0 - k);
                    var tg = c2.G * k + c1.G * (1.0 - k);
                    var tb = c2.B * k + c1.B * (1.0 - k);

                    var blend = ta * InvertedByte;

                    // Blend the colors back with original colors from image
                    row[p]     = (byte)(b * (1.0 - blend) + tb * blend);    //Blue  0-255
                    row[p + 1] = (byte)(g * (1.0 - blend) + tg * blend);    //Green 0-255
                    row[p + 2] = (byte)(r * (1.0 - blend) + tr * blend);    //Red   0-255
                }
            }
        }

        private unsafe void DrawRadialGradient(BitmapData bitmap, Settings settings)
        {
            // Declare origin and end positions as relative to image size
            var origin = new Vector(settings.Start.X * bitmap.Width, settings.Start.Y * bitmap.Height);
            var end = new Vector(settings.End.X * bitmap.Width, settings.End.Y * bitmap.Height);

            // This variable is an optimization
            // It precalculates the distance of the endpoint inversely so that
            // code can run using a multiplication instead of a division
            // as you might now multiplications are way faster
            var invradius = 1.0 / new Vector(origin.X - end.X, origin.Y - end.Y).Length();

            var c1 = settings.FirstColor;
            var c2 = settings.SecondColor;

            var clamp = settings.Clamp;

            for (var y = 0; y < bitmap.Height; y++)
            {
                var row = (byte*)bitmap.Scan0 + (y * bitmap.Stride);

                for (var x = 0; x < bitmap.Width; x++)
                {
                    var p = x * ByteSize;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    // Establish a vector from origin to current position
                    var fp = new Vector(x - origin.X, y - origin.Y);

                    // This gets a value k 0 - 1 based on the distance from origin
                    // Becomes a circular form by design
                    var k = Math.Sqrt(fp.X*fp.X + fp.Y*fp.Y) * invradius;

                    if (clamp > 0)
                        k += -clamp + (k * clamp);

                    // If colors are opaque, no need for mixing.
                    // This assigns the colors directly
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

                    // Mix colors based on k-value, linearly
                    var ta = c2.A * k + c1.A * (1.0 - k);
                    var tr = c2.R * k + c1.R * (1.0 - k);
                    var tg = c2.G * k + c1.G * (1.0 - k);
                    var tb = c2.B * k + c1.B * (1.0 - k);

                    var blend = ta * InvertedByte;

                    // Blend the colors back with original colors from image
                    row[p]     = (byte)(b * (1.0 - blend) + tb * blend);    //Blue  0-255
                    row[p + 1] = (byte)(g * (1.0 - blend) + tg * blend);    //Green 0-255
                    row[p + 2] = (byte)(r * (1.0 - blend) + tr * blend);    //Red   0-255
                }
            }
        }
       
    }
}
