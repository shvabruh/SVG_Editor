using SVG_Editor.Enums;
using SVG_Editor.Shapes;

namespace SVG_Editor
{
    public static class SelectionRenderer
    {
        private const float HandleSize = 6f;

        public static void DrawFrame(Graphics g, IShape s)
        {
            using var pen = new Pen(Color.Black)
            { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            var b = s.Bounds;
            g.DrawRectangle(pen, b.X, b.Y, b.Width, b.Height);

            // для группы ручки не рисуем (нет ресайза группы)
            if (s is GroupShape)
                return;

            foreach (HandleKind h in Enum.GetValues(typeof(HandleKind)))
            {
                if (h == HandleKind.None) continue;
                var r = HandleRect(b, h);
                using var br = new SolidBrush(Color.White);
                using var p2 = new Pen(Color.Black, 1);
                g.FillRectangle(br, r);
                g.DrawRectangle(p2, r.X, r.Y, r.Width, r.Height);
            }
        }

        public static HandleKind HitHandle(IShape s, PointF p)
        {
            if (s is GroupShape) return HandleKind.None;

            var b = s.Bounds;
            foreach (HandleKind h in Enum.GetValues(typeof(HandleKind)))
            {
                if (h == HandleKind.None) continue;
                if (HandleRect(b, h).Contains(p)) return h;
            }
            return HandleKind.None;
        }

        public static RectangleF RectFromTwoPoints(PointF a, PointF b)
        {
            var x = Math.Min(a.X, b.X);
            var y = Math.Min(a.Y, b.Y);
            var w = Math.Abs(a.X - b.X);
            var h = Math.Abs(a.Y - b.Y);
            return new RectangleF(x, y, w, h);
        }

        public static RectangleF ResizeByHandle(RectangleF b, HandleKind h, PointF p)
        {
            var x = b.X; var y = b.Y; var w = b.Width; var hh = b.Height;
            switch (h)
            {
                case HandleKind.N: hh = b.Bottom - p.Y; y = p.Y; break;
                case HandleKind.S: hh = p.Y - b.Y; break;
                case HandleKind.W: w = b.Right - p.X; x = p.X; break;
                case HandleKind.E: w = p.X - b.X; break;
                case HandleKind.NW: y = p.Y; hh = b.Bottom - p.Y; x = p.X; w = b.Right - p.X; break;
                case HandleKind.NE: y = p.Y; hh = b.Bottom - p.Y; w = p.X - b.X; break;
                case HandleKind.SW: hh = p.Y - b.Y; x = p.X; w = b.Right - p.X; break;
                case HandleKind.SE: hh = p.Y - b.Y; w = p.X - b.X; break;
            }
            return new RectangleF(x, y, w, hh);
        }

        public static RectangleF Normalize(RectangleF r)
        {
            var x = r.X; var y = r.Y; var w = r.Width; var h = r.Height;
            if (w < 0) { x += w; w = -w; }
            if (h < 0) { y += h; h = -h; }
            return new RectangleF(x, y, w, h);
        }

        public static RectangleF KeepAspect(RectangleF current, RectangleF original)
        {
            if (original.Width == 0 || original.Height == 0)
                return current;

            float ratio = original.Width / original.Height;
            float w = current.Width;
            float h = current.Height;

            if (w / Math.Max(h, 1e-3f) > ratio)
                w = h * ratio;
            else
                h = w / Math.Max(ratio, 1e-3f);

            return new RectangleF(current.X, current.Y, w, h);
        }


        private static RectangleF HandleRect(RectangleF b, HandleKind h)
        {
            double cx, cy;
            switch (h)
            {
                case HandleKind.N:
                    cx = b.X + b.Width / 2; cy = b.Y; break;
                case HandleKind.S:
                    cx = b.X + b.Width / 2; cy = b.Bottom; break;
                case HandleKind.W:
                    cx = b.X; cy = b.Y + b.Height / 2; break;
                case HandleKind.E:
                    cx = b.Right; cy = b.Y + b.Height / 2; break;
                case HandleKind.NW:
                    cx = b.X; cy = b.Y; break;
                case HandleKind.NE:
                    cx = b.Right; cy = b.Y; break;
                case HandleKind.SW:
                    cx = b.X; cy = b.Bottom; break;
                case HandleKind.SE:
                    cx = b.Right; cy = b.Bottom; break;
                default:
                    cx = b.X; cy = b.Y; break;
            }
            return new RectangleF((float)cx - HandleSize / 2, (float)cy - HandleSize / 2, HandleSize, HandleSize);
        }
    }
}
