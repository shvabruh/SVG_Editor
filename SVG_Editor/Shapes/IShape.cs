using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    public interface IShape
    {
        RectangleF Bounds { get; set; }
        void Draw(Graphics g);
        bool HitTest(PointF p);
        void MoveBy(PointF d);
        Color Fill { get; set; }
        Color Stroke { get; set; }
        float StrokeWidth { get; set; }
        IShape Clone();
    }
}
