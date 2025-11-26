using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    public sealed class LineShape: IShape
    {
        public PointF P1 { get; set; }
        public PointF P2 { get; set; }
        public Color Fill { get; set; } = Color.Transparent; // не используется
        public Color Stroke { get; set; } = Color.Black;
        public float StrokeWidth { get; set; } = 2f;

        public LineShape(PointF p1, PointF p2) { P1 = p1; P2 = p2; }

        public RectangleF Bounds
        {
            get
            {
                var x = Math.Min(P1.X, P2.X);
                var y = Math.Min(P1.Y, P2.Y);
                var w = Math.Abs(P1.X - P2.X);
                var h = Math.Abs(P1.Y - P2.Y);
                return new RectangleF(x, y, w, h);
            }
            set
            {
                P2 = new PointF(value.Right, value.Bottom);
            }
        }

        public void Draw(Graphics g)
        {
            using var pen = new Pen(Stroke, StrokeWidth)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            g.DrawLine(pen, P1, P2);
        }

        public bool HitTest(PointF p)
        {
            float tol = Math.Max(4f, StrokeWidth);
            return Geometry.DistancePointToSegment(p, P1, P2) <= tol;
        }

        public void MoveBy(PointF d)
        {
            P1 = new PointF(P1.X + d.X, P1.Y + d.Y);
            P2 = new PointF(P2.X + d.X, P2.Y + d.Y);
        }

        public IShape Clone() =>
            new LineShape(P1, P2)
            {
                Stroke = this.Stroke,
                StrokeWidth = this.StrokeWidth
            };
    }
}
