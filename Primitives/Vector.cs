using System;

namespace ImageResizer.Plugins.GradientOverlay.Primitives
{
    public struct Vector : IEquatable<Vector>
    {
        private const double Precision = 0.001;
        private const int InvertedPrecision = (int) (1 / Precision);
        private const int HashPrimeOne = 41;
        private const int HashPrimeTwo = 97;

        public readonly double X;
        public readonly double Y;

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vector other)
        {
            return Math.Abs(other.X - X) < Precision &&
                   Math.Abs(other.Y - Y) < Precision;
        }

        // More efficient equals
        // Avoids unboxing
        public override bool Equals(object obj)
        {
            if (!(obj is Vector)) return false;
            
            var other = (Vector) obj;
            
            return Math.Abs(other.X - X) < Precision && 
                   Math.Abs(other.Y - Y) < Precision;
        }

        // Note that hash is generated in such a way
        // that vectors falling under the same precision
        // are matched in list contains.
        public override int GetHashCode()
        {
            var hash = HashPrimeOne;

            hash *= HashPrimeTwo + (int)(X * InvertedPrecision);
            hash *= HashPrimeTwo + (int)(Y * InvertedPrecision);

            return hash;
        }
    }
}