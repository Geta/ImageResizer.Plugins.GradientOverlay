using System.Drawing;
using ImageResizer.Plugins.GradientOverlay.Primitives;

namespace ImageResizer.Plugins.GradientOverlay
{
    public class Settings 
    {
        public bool Radial { get; set; }

        public Vector Start { get; set; }
        public Vector End { get; set; }

        public double Clamp { get; set; }

        public Color FirstColor { get; set; }
        public Color SecondColor { get; set; }
    }
}
