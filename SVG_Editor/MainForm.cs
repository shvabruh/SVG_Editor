using SVG_Editor.Commands;
using SVG_Editor.Enums;
using SVG_Editor.Shapes;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace SVG_Editor
{
    public sealed partial class MainForm : Form
    {
        enum Tool { Select, Rect, Ellipse, Line }
        enum HandleKind { None, N, NE, E, SE, S, SW, W, NW }

        private readonly List<IShape> _shapes = new();
        private readonly History _history = new();
        private Tool _tool = Tool.Select;

        private IShape? _selection;
        private IShape? _clipboard;
        private readonly HashSet<IShape> _multiSelection = new();

        private HandleKind _activeHandle = HandleKind.None;
        private PointF _dragStartCanvas;
        private RectangleF _startBounds;
        private PointF _netDelta;
        private LineShape? _newLine;
        private SizeF _canvasSize = new(1200, 800);

        // viewport (пока без масштабирования/панорамы)
        private float _zoom = 1f;
        private PointF _pan = new(0, 0);

        // UI
        private readonly ToolStrip _ts = new();
        private readonly StatusStrip _ss = new();
        private readonly ToolStripStatusLabel _status = new();

        // Пипетка стиля
        private bool _stylePickMode;
        private bool _styleBuffered;
        private Color _styleFill;
        private Color _styleStroke;
        private float _styleStrokeWidth;

        public MainForm()
        {
            Text = "SVG Editor";
            DoubleBuffered = true;
            ClientSize = new Size(1000, 700);
            KeyPreview = true;

            // ===== Меню =====
            var miOpen = new ToolStripMenuItem("Open", null, (_, __) => DoOpen())
            { ShortcutKeys = Keys.Control | Keys.O };
            var miSave = new ToolStripMenuItem("Save", null, (_, __) => DoSave())
            { ShortcutKeys = Keys.Control | Keys.S };
            var miUndo = new ToolStripMenuItem("Undo", null, (_, __) => { _history.Undo(); Invalidate(); })
            { ShortcutKeys = Keys.Control | Keys.Z };
            var miRedo = new ToolStripMenuItem("Redo", null, (_, __) => { _history.Redo(); Invalidate(); })
            { ShortcutKeys = Keys.Control | Keys.Y };
            var miDel = new ToolStripMenuItem("Delete", null, (_, __) => DeleteSelection())
            { ShortcutKeys = Keys.Delete };

            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("File");
            file.DropDownItems.AddRange(new[] { miOpen, miSave });
            var edit = new ToolStripMenuItem("Edit");
            edit.DropDownItems.AddRange(new[] { miUndo, miRedo, miDel });
            menu.Items.AddRange(new[] { file, edit });
            MainMenuStrip = menu;
            Controls.Add(menu);

            // ===== ToolStrip =====
            var bOpen = new ToolStripButton("Open");
            var bSave = new ToolStripButton("Save");
            var bUndo = new ToolStripButton("Undo");
            var bRedo = new ToolStripButton("Redo");
            var bDel = new ToolStripButton("Delete");

            var bSelect = new ToolStripButton("Select") { CheckOnClick = true, Checked = true };
            var bRect = new ToolStripButton("Rect") { CheckOnClick = true };
            var bEll = new ToolStripButton("Ellipse") { CheckOnClick = true };
            var bLine = new ToolStripButton("Line") { CheckOnClick = true };

            bOpen.Click += (_, __) => DoOpen();
            bSave.Click += (_, __) => DoSave();
            bUndo.Click += (_, __) => { _history.Undo(); Invalidate(); };
            bRedo.Click += (_, __) => { _history.Redo(); Invalidate(); };
            bDel.Click += (_, __) => DeleteSelection();

            bSelect.Click += (_, __) => SetTool(Tool.Select, bSelect, bRect, bEll, bLine);
            bRect.Click += (_, __) => SetTool(Tool.Rect, bSelect, bRect, bEll, bLine);
            bEll.Click += (_, __) => SetTool(Tool.Ellipse, bSelect, bRect, bEll, bLine);
            bLine.Click += (_, __) => SetTool(Tool.Line, bSelect, bRect, bEll, bLine);

            _ts.GripStyle = ToolStripGripStyle.Hidden;
            _ts.Items.AddRange(new ToolStripItem[]
            {
                bOpen, bSave, new ToolStripSeparator(),
                bUndo, bRedo, new ToolStripSeparator(),
                bSelect, bRect, bEll, bLine, new ToolStripSeparator(),
                bDel
            });
            Controls.Add(_ts);

            // ===== Статус =====
            _ss.Items.Add(_status);
            Controls.Add(_ss);

            // События
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += OnKeyDown;
        }

        private void SetTool(Tool t, ToolStripButton bSelect, ToolStripButton bRect, ToolStripButton bEll, ToolStripButton bLine)
        {
            _tool = t;
            bSelect.Checked = t == Tool.Select;
            bRect.Checked = t == Tool.Rect;
            bEll.Checked = t == Tool.Ellipse;
            bLine.Checked = t == Tool.Line;
            _status.Text = $"Tool: {_tool}" + (_stylePickMode ? "  |  Пипетка" : "");
        }

        // ===== Рендеринг =====
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Color.White);

            int topOffset = _ts.Height + MainMenuStrip!.Height;
            using (var pen = new Pen(Color.LightGray, 1))
                g.DrawRectangle(pen, 0, topOffset, (int)_canvasSize.Width, (int)_canvasSize.Height);

            g.TranslateTransform(0, topOffset);

            foreach (var s in _shapes)
                s.Draw(g);

            // рамка выделения: если есть множественный выбор — рисуем рамки всем
            if (_multiSelection.Count > 0)
            {
                foreach (var s in _multiSelection)
                    SelectionRenderer.DrawFrame(g, s);
            }
            else if (_selection is not null)
            {
                SelectionRenderer.DrawFrame(g, _selection);
            }
        }

        // ===== Мышь =====
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            var pCanvas = ScreenToCanvas(e.Location);
            _dragStartCanvas = pCanvas;
            _netDelta = default;

            if (e.Button != MouseButtons.Left)
                return;

            if (_tool == Tool.Select)
            {
                // 1) ручки ресайза
                if (_selection is not null)
                {
                    _activeHandle = (HandleKind)SelectionRenderer.HitHandle(_selection, pCanvas);
                    if (_activeHandle != HandleKind.None)
                    {
                        _startBounds = _selection.Bounds;
                        return;
                    }
                }

                // 2) режим пипетки
                if (_stylePickMode)
                {
                    var hit = HitTest(pCanvas);
                    if (hit is null)
                    {
                        _status.Text = "Пипетка: нет фигуры под курсором";
                        return;
                    }

                    if (!_styleBuffered)
                    {
                        _styleFill = hit.Fill;
                        _styleStroke = hit.Stroke;
                        _styleStrokeWidth = hit.StrokeWidth;
                        _styleBuffered = true;
                        _status.Text = "Пипетка: стиль взят, кликните по другой фигуре, чтобы применить";
                    }
                    else
                    {
                        var cmd = new ApplyStyleCommand(hit, _styleFill, _styleStroke, _styleStrokeWidth);
                        _history.Exec(cmd);
                        _stylePickMode = false;
                        _status.Text = "Пипетка: стиль применён";
                        Invalidate();
                    }
                    return;
                }

                // 3) множественное выделение по Shift+клик
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    var hit = HitTest(pCanvas);
                    if (hit is not null)
                    {
                        if (_multiSelection.Contains(hit))
                            _multiSelection.Remove(hit);
                        else
                            _multiSelection.Add(hit);

                        _selection = hit;
                        Invalidate();
                    }
                    return;
                }

                // 4) обычное одиночное выделение
                _multiSelection.Clear();
                _selection = HitTest(pCanvas);
                if (_selection is not null)
                    _startBounds = _selection.Bounds;
                Invalidate();
            }
            else if (_tool == Tool.Rect)
            {
                var r = new RectShape(new RectangleF(pCanvas, SizeF.Empty));
                _shapes.Add(r);
                _selection = r;
                _startBounds = r.Bounds;
            }
            else if (_tool == Tool.Ellipse)
            {
                var el = new EllipseShape(new RectangleF(pCanvas, SizeF.Empty));
                _shapes.Add(el);
                _selection = el;
                _startBounds = el.Bounds;
            }
            else if (_tool == Tool.Line)
            {
                _newLine = new LineShape(pCanvas, pCanvas);
                _shapes.Add(_newLine);
                _selection = _newLine;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            var pCanvas = ScreenToCanvas(e.Location);
            _status.Text = $"{_tool}  |  p=({pCanvas.X:F1},{pCanvas.Y:F1})" + (_stylePickMode ? "  |  Пипетка" : "");

            if (e.Button != MouseButtons.Left)
                return;

            if (_tool == Tool.Select && _selection is not null)
            {
                if (_activeHandle != HandleKind.None && _selection is not GroupShape)
                {
                    // ресайз (для групп не даём ручек)
                    var b = SelectionRenderer.ResizeByHandle(_startBounds, (Enums.HandleKind)_activeHandle, pCanvas);
                    if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                        b = SelectionRenderer.KeepAspect(b, _startBounds);
                    _selection.Bounds = SelectionRenderer.Normalize(b);
                    Invalidate();
                }
                else
                {
                    // перемещение — либо всех фигур в multiSelection, либо одной _selection
                    var delta = new PointF(pCanvas.X - _dragStartCanvas.X, pCanvas.Y - _dragStartCanvas.Y);

                    // если у нас есть мультивыделение, убедимся, что текущая фигура тоже там
                    if (_multiSelection.Count > 0)
                    {
                        if (!_multiSelection.Contains(_selection))
                            _multiSelection.Add(_selection);

                        foreach (var shape in _multiSelection)
                            shape.MoveBy(delta);
                    }
                    else
                    {
                        // обычное одиночное перемещение
                        _selection.MoveBy(delta);
                    }

                    _netDelta = new PointF(_netDelta.X + delta.X, _netDelta.Y + delta.Y);
                    _dragStartCanvas = pCanvas;
                    Invalidate();
                }
            }
            else if (_tool == Tool.Rect && _selection is RectShape r)
            {
                var b = SelectionRenderer.RectFromTwoPoints(_dragStartCanvas, pCanvas);
                r.Bounds = b;
                Invalidate();
            }
            else if (_tool == Tool.Ellipse && _selection is EllipseShape el)
            {
                var b = SelectionRenderer.RectFromTwoPoints(_dragStartCanvas, pCanvas);
                el.Bounds = b;
                Invalidate();
            }
            else if (_tool == Tool.Line && _newLine is not null)
            {
                _newLine.P2 = pCanvas;
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (_tool == Tool.Select && _selection is not null)
            {
                if (_activeHandle != HandleKind.None && _selection is not GroupShape)
                {
                    var cmd = new ResizeCommand(_selection, _startBounds, _selection.Bounds);
                    _history.Exec(cmd);
                    _activeHandle = HandleKind.None;
                }
                else if (Math.Abs(_netDelta.X) > 0.01f || Math.Abs(_netDelta.Y) > 0.01f)
                {
                    var cmd = new MoveCommand(_selection, _netDelta);
                    _history.Exec(cmd);
                }
            }
            else if (_tool == Tool.Rect || _tool == Tool.Ellipse)
            {
                if (_selection is not null)
                    _history.Exec(new AddShapeCommand(_shapes, _selection));

                SetTool(Tool.Select,
                    (ToolStripButton)_ts.Items[6],
                    (ToolStripButton)_ts.Items[7],
                    (ToolStripButton)_ts.Items[8],
                    (ToolStripButton)_ts.Items[9]);
            }
            else if (_tool == Tool.Line)
            {
                if (_newLine is not null)
                {
                    _history.Exec(new AddShapeCommand(_shapes, _newLine));
                    _newLine = null;
                    SetTool(Tool.Select,
                        (ToolStripButton)_ts.Items[6],
                        (ToolStripButton)_ts.Items[7],
                        (ToolStripButton)_ts.Items[8],
                        (ToolStripButton)_ts.Items[9]);
                }
            }

            Invalidate();
        }

        // ===== Клавиатура / хоткеи =====
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Delete
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelection();
                return;
            }

            // Инструменты: V/R/E/L (без Ctrl)
            if (!e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.V)
                {
                    SetTool(Tool.Select,
                        (ToolStripButton)_ts.Items[6],
                        (ToolStripButton)_ts.Items[7],
                        (ToolStripButton)_ts.Items[8],
                        (ToolStripButton)_ts.Items[9]);
                    return;
                }
                if (e.KeyCode == Keys.R)
                {
                    SetTool(Tool.Rect,
                        (ToolStripButton)_ts.Items[6],
                        (ToolStripButton)_ts.Items[7],
                        (ToolStripButton)_ts.Items[8],
                        (ToolStripButton)_ts.Items[9]);
                    return;
                }
                if (e.KeyCode == Keys.E)
                {
                    SetTool(Tool.Ellipse,
                        (ToolStripButton)_ts.Items[6],
                        (ToolStripButton)_ts.Items[7],
                        (ToolStripButton)_ts.Items[8],
                        (ToolStripButton)_ts.Items[9]);
                    return;
                }
                if (e.KeyCode == Keys.L)
                {
                    SetTool(Tool.Line,
                        (ToolStripButton)_ts.Items[6],
                        (ToolStripButton)_ts.Items[7],
                        (ToolStripButton)_ts.Items[8],
                        (ToolStripButton)_ts.Items[9]);
                    return;
                }

                // Пипетка: I
                if (e.KeyCode == Keys.I)
                {
                    _stylePickMode = !_stylePickMode;
                    _styleBuffered = false;
                    _status.Text = _stylePickMode
                        ? "Пипетка: клик по фигуре, чтобы взять стиль"
                        : $"Tool: {_tool}";
                    return;
                }
            }

            // Copy / Paste
            if (e.Control && e.KeyCode == Keys.C)
            {
                _clipboard = _selection?.Clone();
                return;
            }
            if (e.Control && e.KeyCode == Keys.V && _clipboard is not null)
            {
                var pasted = _clipboard.Clone();
                var b = pasted.Bounds;
                pasted.Bounds = new RectangleF(b.X + 10, b.Y + 10, b.Width, b.Height);
                _history.Exec(new AddShapeCommand(_shapes, pasted));
                _selection = pasted;
                _multiSelection.Clear();
                Invalidate();
                return;
            }

            // Дубликат (Ctrl + D)
            if (e.Control && e.KeyCode == Keys.D && _selection is not null)
            {
                var dup = _selection.Clone();
                var b = dup.Bounds;
                dup.Bounds = new RectangleF(b.X + 12, b.Y + 12, b.Width, b.Height);
                _history.Exec(new AddShapeCommand(_shapes, dup));
                _selection = dup;
                _multiSelection.Clear();
                Invalidate();
                return;
            }

            // Группировка (Ctrl + G)
            if (e.Control && !e.Shift && e.KeyCode == Keys.G)
            {
                var set = new HashSet<IShape>(_multiSelection);
                if (_selection is not null)
                    set.Add(_selection);

                if (set.Count >= 2)
                {
                    var cmd = new GroupCommand(_shapes, set);
                    _history.Exec(cmd);
                    _selection = cmd.Group;
                    _multiSelection.Clear();
                    Invalidate();
                }
                return;
            }

            // Разгруппировка (Ctrl + Shift + G)
            if (e.Control && e.Shift && e.KeyCode == Keys.G && _selection is GroupShape gs)
            {
                var cmd = new UngroupCommand(_shapes, gs);
                _history.Exec(cmd);
                _selection = null;
                _multiSelection.Clear();
                Invalidate();
                return;
            }

            // Подвигать стрелками (1px, с Shift — 10px)
            if (_selection is not null &&
                (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                 e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
            {
                int step = e.Shift ? 10 : 1;
                var d = e.KeyCode switch
                {
                    Keys.Left => new PointF(-step, 0),
                    Keys.Right => new PointF(step, 0),
                    Keys.Up => new PointF(0, -step),
                    Keys.Down => new PointF(0, step),
                    _ => new PointF(0, 0)
                };
                if (d.X != 0 || d.Y != 0)
                {
                    _history.Exec(new MoveCommand(_selection, d));
                    Invalidate();
                }
                return;
            }
        }

        private void DeleteSelection()
        {
            if (_selection is null) return;
            _history.Exec(new RemoveShapeCommand(_shapes, _selection));
            _selection = null;
            _multiSelection.Clear();
            Invalidate();
        }

        // ===== Hit-test =====
        private IShape? HitTest(PointF p)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
                if (_shapes[i].HitTest(p)) return _shapes[i];
            return null;
        }

        private PointF ScreenToCanvas(Point p)
        {
            int yOff = _ts.Height + MainMenuStrip!.Height;
            return new PointF(
                (p.X - _pan.X) / _zoom,
                (p.Y - yOff - _pan.Y) / _zoom);
        }

        // ===== SVG I/O =====
        private void DoSave()
        {
            using var sfd = new SaveFileDialog() { Filter = "SVG files (*.svg)|*.svg" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var root = new XElement("svg",
                new XAttribute("xmlns", "http://www.w3.org/2000/svg"),
                new XAttribute("width", _canvasSize.Width),
                new XAttribute("height", _canvasSize.Height));

            foreach (var sh in _shapes)
                SaveShapeToSvg(root, sh);

            new XDocument(root).Save(sfd.FileName);
        }

        private void SaveShapeToSvg(XElement parent, IShape sh)
        {
            switch (sh)
            {
                case RectShape r:
                    parent.Add(new XElement("rect",
                        new XAttribute("x", r.Bounds.X),
                        new XAttribute("y", r.Bounds.Y),
                        new XAttribute("width", r.Bounds.Width),
                        new XAttribute("height", r.Bounds.Height),
                        new XAttribute("fill", ColorTranslator.ToHtml(r.Fill)),
                        new XAttribute("stroke", ColorTranslator.ToHtml(r.Stroke)),
                        new XAttribute("stroke-width", r.StrokeWidth)));
                    break;

                case EllipseShape el:
                    var cx = el.Bounds.X + el.Bounds.Width / 2f;
                    var cy = el.Bounds.Y + el.Bounds.Height / 2f;
                    parent.Add(new XElement("ellipse",
                        new XAttribute("cx", cx),
                        new XAttribute("cy", cy),
                        new XAttribute("rx", el.Bounds.Width / 2f),
                        new XAttribute("ry", el.Bounds.Height / 2f),
                        new XAttribute("fill", ColorTranslator.ToHtml(el.Fill)),
                        new XAttribute("stroke", ColorTranslator.ToHtml(el.Stroke)),
                        new XAttribute("stroke-width", el.StrokeWidth)));
                    break;

                case LineShape ln:
                    parent.Add(new XElement("line",
                        new XAttribute("x1", ln.P1.X),
                        new XAttribute("y1", ln.P1.Y),
                        new XAttribute("x2", ln.P2.X),
                        new XAttribute("y2", ln.P2.Y),
                        new XAttribute("stroke", ColorTranslator.ToHtml(ln.Stroke)),
                        new XAttribute("stroke-width", ln.StrokeWidth)));
                    break;

                case GroupShape g:
                    // группы в SVG пока «плющим» в набор отдельных фигур (без <g>)
                    foreach (var c in g.Children)
                        SaveShapeToSvg(parent, c);
                    break;
            }
        }

        private void DoOpen()
        {
            using var ofd = new OpenFileDialog() { Filter = "SVG files (*.svg)|*.svg" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var doc = XDocument.Load(ofd.FileName);
            var root = doc.Root ?? throw new Exception("Invalid SVG");
            _shapes.Clear();

            var wAttr = root.Attribute("width");
            var hAttr = root.Attribute("height");
            if (wAttr != null && hAttr != null &&
                float.TryParse(wAttr.Value, out var w) &&
                float.TryParse(hAttr.Value, out var h))
                _canvasSize = new SizeF(w, h);

            foreach (var el in root.Elements())
            {
                switch (el.Name.LocalName)
                {
                    case "rect":
                        var rx = ParseF(el, "x");
                        var ry = ParseF(el, "y");
                        var rw = ParseF(el, "width");
                        var rh = ParseF(el, "height");
                        var r = new RectShape(new RectangleF(rx, ry, rw, rh))
                        {
                            Fill = ParseColor(el, "fill", Color.Transparent),
                            Stroke = ParseColor(el, "stroke", Color.Black),
                            StrokeWidth = ParseF(el, "stroke-width", 1f)
                        };
                        _shapes.Add(r);
                        break;

                    case "ellipse":
                        var cx = ParseF(el, "cx");
                        var cy = ParseF(el, "cy");
                        var rx2 = ParseF(el, "rx");
                        var ry2 = ParseF(el, "ry");
                        var e = new EllipseShape(new RectangleF(cx - rx2, cy - ry2, rx2 * 2, ry2 * 2))
                        {
                            Fill = ParseColor(el, "fill", Color.Transparent),
                            Stroke = ParseColor(el, "stroke", Color.Black),
                            StrokeWidth = ParseF(el, "stroke-width", 1f)
                        };
                        _shapes.Add(e);
                        break;

                    case "line":
                        var x1 = ParseF(el, "x1");
                        var y1 = ParseF(el, "y1");
                        var x2 = ParseF(el, "x2");
                        var y2 = ParseF(el, "y2");
                        var l = new LineShape(new PointF(x1, y1), new PointF(x2, y2))
                        {
                            Stroke = ParseColor(el, "stroke", Color.Black),
                            StrokeWidth = ParseF(el, "stroke-width", 1f)
                        };
                        _shapes.Add(l);
                        break;
                }
            }

            _selection = null;
            _multiSelection.Clear();
            Invalidate();
        }

        private static float ParseF(XElement el, string name, float def = 0f) =>
            el.Attribute(name) is XAttribute a && float.TryParse(a.Value, out var v) ? v : def;

        private static Color ParseColor(XElement el, string name, Color def) =>
            el.Attribute(name) is XAttribute a ? ColorTranslator.FromHtml(a.Value) : def;
       
    }
}
