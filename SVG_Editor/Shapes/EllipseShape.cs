using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    public sealed class EllipseShape: IShape
    {
        public RectangleF Bounds { get; set; }
        public Color Fill { get; set; } = Color.FromArgb(24, Color.MediumSeaGreen);
        public Color Stroke { get; set; } = Color.SeaGreen;
        public float StrokeWidth { get; set; } = 2f;

        public EllipseShape(RectangleF b) { Bounds = b; }

        public void Draw(Graphics g)
        {
            using var br = new SolidBrush(Fill);
            using var pen = new Pen(Stroke, StrokeWidth);
            g.FillEllipse(br, Bounds);
            g.DrawEllipse(pen, Bounds);
        }

        public bool HitTest(PointF p)
        {
            var cx = Bounds.X + Bounds.Width / 2f;
            var cy = Bounds.Y + Bounds.Height / 2f;
            var rx = Bounds.Width / 2f;
            var ry = Bounds.Height / 2f;
            if (rx <= 0 || ry <= 0) return false;
            var nx = (p.X - cx) / rx;
            var ny = (p.Y - cy) / ry;
            return nx * nx + ny * ny <= 1.0f;
        }

        public void MoveBy(PointF d) =>
            Bounds = new RectangleF(Bounds.X + d.X, Bounds.Y + d.Y, Bounds.Width, Bounds.Height);

        public IShape Clone() =>
            new EllipseShape(Bounds)
            {
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeWidth = this.StrokeWidth
            };
    }
}
