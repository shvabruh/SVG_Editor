using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    public sealed class GroupShape: IShape
    {
        public List<IShape> Children { get; }
        private RectangleF _bounds;

        public RectangleF Bounds
        {
            get => _bounds;
            set => _bounds = value; // внешний сеттер почти не используем
        }

        public Color Fill { get; set; } = Color.Transparent;
        public Color Stroke { get; set; } = Color.Transparent;
        public float StrokeWidth { get; set; } = 0f;

        public GroupShape(IEnumerable<IShape> children)
        {
            Children = children.ToList();
            RecalcBounds();
        }

        public void Draw(Graphics g)
        {
            foreach (var c in Children)
                c.Draw(g);
        }

        public bool HitTest(PointF p) =>
            Children.Any(c => c.HitTest(p));

        public void MoveBy(PointF d)
        {
            foreach (var c in Children)
                c.MoveBy(d);
            RecalcBounds();
        }

        public IShape Clone() =>
            new GroupShape(Children.Select(c => c.Clone()));

        public void RecalcBounds()
        {
            if (Children.Count == 0)
            {
                _bounds = RectangleF.Empty;
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var c in Children)
            {
                var b = c.Bounds;
                if (b.X < minX) minX = b.X;
                if (b.Y < minY) minY = b.Y;
                if (b.Right > maxX) maxX = b.Right;
                if (b.Bottom > maxY) maxY = b.Bottom;
            }

            _bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
