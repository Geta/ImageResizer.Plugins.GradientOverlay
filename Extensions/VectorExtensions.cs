using System;
using ImageResizer.Plugins.GradientOverlay.Primitives;

namespace ImageResizer.Plugins.GradientOverlay.Extensions
{
    public static class VectorExtensions
    {
        public static Vector VectorYFromArray(this int[] array)
        {
            if (array == null) return new Vector(0.0, 0.0);
            if (array.Length < 2) return new Vector(0.0, array[0] * 0.01);
            
            return new Vector(array[0] * 0.01, array[1] * 0.01);
        }

        public static Vector VectorXFromArray(this int[] array)
        {
            if (array == null) return new Vector(0.0, 0.0);
            if (array.Length < 2) return new Vector(array[0] * 0.01, 0.0);

            return new Vector(array[0] * 0.01, array[1] * 0.01);
        }

        public static double Length(this Vector v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        public static Vector Normalize(this Vector v, double l = 1.0)
        {
            var n = l / Length(v);
            return new Vector(v.X * n, v.Y * n);
        }

        public static Vector Rotate(this Vector v, double a)
        {
            return new Vector(Math.Cos(a) * v.X - Math.Sin(a) * v.Y, Math.Sin(a) * v.X + Math.Cos(a) * v.Y);
        }

        public static double Rotation(this Vector v)
        {
            return Math.Atan2(v.X, v.Y);
        }

        public static double ProjectToLine(this Vector p, Vector s, Vector e)
        {
            var ap = new Vector(p.X - s.X, p.Y - s.Y);
            var ab = new Vector(e.X - s.X, e.Y - s.Y);

            return (ap.X * ab.X + ap.Y * ab.Y) / (ab.X * ab.X + ab.Y * ab.Y);
        }
    }
}
