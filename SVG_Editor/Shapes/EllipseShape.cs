using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    /// <summary>
    /// Эллипс с возможной заливкой и обводкой.
    /// При равных размерах по ширине и высоте используется как круг.
    /// </summary>
    public sealed class EllipseShape: IShape
    {
        public RectangleF Bounds { get; set; }
        public Color Fill { get; set; } = Color.Transparent;
        public Color Stroke { get; set; } = Color.SeaGreen;
        public float StrokeWidth { get; set; } = 2f;

        public EllipseShape(RectangleF b) { Bounds = b; }

        /// <summary>
        /// Отрисовывает эллипс, учитывая заливку и обводку.
        /// </summary>
        public void Draw(Graphics g)
        public void Draw(Graphics g)
        {
            if (Fill.A > 0)
            {
                using var br = new SolidBrush(Fill);
                g.FillEllipse(br, Bounds);
            }

            if (Stroke.A > 0 && StrokeWidth > 0)
            {
                using var pen = new Pen(Stroke, StrokeWidth);
                g.DrawEllipse(pen, Bounds);
            }
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
