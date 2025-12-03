using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    /// <summary>
    /// Прямоугольная фигура с возможной заливкой и обводкой.
    /// Используется для рисования прямоугольников и квадратов.
    /// </summary>
    public sealed class RectShape : IShape
    {
        public RectangleF Bounds { get; set; }
        public Color Fill { get; set; } = Color.Transparent;
        public Color Stroke { get; set; } = Color.DodgerBlue;
        public float StrokeWidth { get; set; } = 2f;

        public RectShape(RectangleF b) { Bounds = b; }

        /// <summary>
        /// Рисует прямоугольник на холсте:
        /// сначала заливку (если она есть), затем обводку.
        /// </summary>
        public void Draw(Graphics g)
        {
            if (Fill.A > 0)
            {
                using var br = new SolidBrush(Fill);
                g.FillRectangle(br, Bounds);
            }

            if (Stroke.A > 0 && StrokeWidth > 0)
            {
                using var pen = new Pen(Stroke, StrokeWidth);
                g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
            }  
        }

        public bool HitTest(PointF p) => Bounds.Contains(p);

        public void MoveBy(PointF d) =>
            Bounds = new RectangleF(Bounds.X + d.X, Bounds.Y + d.Y, Bounds.Width, Bounds.Height);

        public IShape Clone() =>
            new RectShape(Bounds)
            {
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeWidth = this.StrokeWidth
            };
    }
}
