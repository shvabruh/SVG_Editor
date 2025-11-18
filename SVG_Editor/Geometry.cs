using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor
{
    public static class Geometry
    {
        public static float DistancePointToSegment(PointF p, PointF a, PointF b)
        {
            var vx = b.X - a.X; var vy = b.Y - a.Y;
            var wx = p.X - a.X; var wy = p.Y - a.Y;
            var c1 = vx * wx + vy * wy;
            if (c1 <= 0) return Distance(p, a);
            var c2 = vx * vx + vy * vy;
            if (c2 <= c1) return Distance(p, b);
            var t = c1 / c2;
            var proj = new PointF(a.X + t * vx, a.Y + t * vy);
            return Distance(p, proj);
        }

        public static float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
