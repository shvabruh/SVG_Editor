using SVG_Editor.Commands;
using SVG_Editor.Shapes;
using System.Xml.Linq;
using System.Globalization;

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
        private ToolStripButton _btnSelect = null!;
        private ToolStripButton _btnRect = null!;
        private ToolStripButton _btnEll = null!;
        private ToolStripButton _btnLine = null!;

        // Пипетка стиля
        private bool _stylePickMode;
        private bool _styleBuffered;
        private Color _styleFill;
        private Color _styleStroke;
        private float _styleStrokeWidth;

        // Панель свойств
        private Panel _propsPanel = null!;
        private TextBox _tbX = null!;
        private TextBox _tbY = null!;
        private TextBox _tbW = null!;
        private TextBox _tbH = null!;
        private Button _btnFill = null!;
        private Button _btnStroke = null!;
        private NumericUpDown _numStrokeWidth = null!;
        private readonly ColorDialog _colorDialog = new();
        private bool _updatingPropsFromSelection;

        private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

        public MainForm()
        {
            Text = "SVG-редактор";
            DoubleBuffered = true;
            ClientSize = new Size(1000, 700);
            KeyPreview = true;

            // ===== Меню =====
            var miNew = new ToolStripMenuItem("Новый холст", null, (_, __) => DoNewCanvas())
            { ShortcutKeys = Keys.Control | Keys.O };
            var miOpen = new ToolStripMenuItem("Открыть", null, (_, __) => DoOpen())
            { ShortcutKeys = Keys.Control | Keys.O };
            var miSave = new ToolStripMenuItem("Сохранить", null, (_, __) => DoSave())
            { ShortcutKeys = Keys.Control | Keys.S };
            var miUndo = new ToolStripMenuItem("Отменить", null, (_, __) => { _history.Undo(); Invalidate(); })
            { ShortcutKeys = Keys.Control | Keys.Z };
            var miRedo = new ToolStripMenuItem("Повторить", null, (_, __) => { _history.Redo(); Invalidate(); })
            { ShortcutKeys = Keys.Control | Keys.Y };
            var miDel = new ToolStripMenuItem("Удалить", null, (_, __) => DeleteSelection())
            { ShortcutKeys = Keys.Delete };

            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("Файл");
            file.DropDownItems.AddRange(new[] { miOpen, miSave });
            file.DropDownItems.Insert(0, miNew);
            var edit = new ToolStripMenuItem("Правка");
            edit.DropDownItems.AddRange(new[] { miUndo, miRedo, miDel });
            menu.Items.AddRange(new[] { file, edit });
            MainMenuStrip = menu;
            Controls.Add(menu);

            // ===== ToolStrip =====
            var bNew = new ToolStripButton("Новый холст");
            var bOpen = new ToolStripButton("Открыть");
            var bSave = new ToolStripButton("Сохранить");
            var bUndo = new ToolStripButton("Отменить");
            var bRedo = new ToolStripButton("Повторить");
            var bDel = new ToolStripButton("Удалить");

            _btnSelect = new ToolStripButton("Выбор") { CheckOnClick = true, Checked = true };
            _btnRect = new ToolStripButton("Прямоугольник") { CheckOnClick = true };
            _btnEll = new ToolStripButton("Эллипс") { CheckOnClick = true };
            _btnLine = new ToolStripButton("Линия") { CheckOnClick = true };

            bNew.Click += (_, __) => DoNewCanvas();
            bOpen.Click += (_, __) => DoOpen();
            bSave.Click += (_, __) => DoSave();
            bUndo.Click += (_, __) => { _history.Undo(); Invalidate(); };
            bRedo.Click += (_, __) => { _history.Redo(); Invalidate(); };
            bDel.Click += (_, __) => DeleteSelection();

            _btnSelect.Click += (_, __) => SetTool(Tool.Select);
            _btnRect.Click += (_, __) => SetTool(Tool.Rect);
            _btnEll.Click += (_, __) => SetTool(Tool.Ellipse);
            _btnLine.Click += (_, __) => SetTool(Tool.Line);

            _ts.GripStyle = ToolStripGripStyle.Hidden;
            _ts.Items.AddRange(new ToolStripItem[]
            {
                bNew, bOpen, bSave, new ToolStripSeparator(),
                bUndo, bRedo, new ToolStripSeparator(),
                _btnSelect, _btnRect, _btnEll, _btnLine, new ToolStripSeparator(),
                bDel
            });
            Controls.Add(_ts);

            // ===== Статус =====
            _ss.Items.Add(_status);
            Controls.Add(_ss);

            // ===== Панель свойств справа =====
            _propsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 220,
                Padding = new Padding(8),
                BackColor = SystemColors.ControlLight
            };

            var lblTitle = new Label { Text = "Свойства", Dock = DockStyle.Top, Font = new Font(Font, FontStyle.Bold), Height = 24 };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = false
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            void AddRow(string label, Control editor)
            {
                int row = table.RowCount - 1;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                var l = new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
                editor.Dock = DockStyle.Fill;
                table.Controls.Add(l, 0, row);
                table.Controls.Add(editor, 1, row);
                table.RowCount++;
            }

            // координаты и размеры
            _tbX = new TextBox();
            _tbY = new TextBox();
            _tbW = new TextBox();
            _tbH = new TextBox();

            AddRow("X:", _tbX);
            AddRow("Y:", _tbY);
            AddRow("Ширина:", _tbW);
            AddRow("Высота:", _tbH);

            // заливка / обводка
            _btnFill = new Button { Text = "Выбрать..." };
            _btnStroke = new Button { Text = "Выбрать..." };
            AddRow("Заливка:", _btnFill);
            AddRow("Обводка:", _btnStroke);

            // толщина обводки
            _numStrokeWidth = new NumericUpDown { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Increment = 0.5M };
            AddRow("Толщина:", _numStrokeWidth);

            // события
            _btnFill.Click += (_, __) => ChangeColor(fill: true);
            _btnStroke.Click += (_, __) => ChangeColor(fill: false);

            _tbX.Leave += (_, __) => ApplyPropsFromPanel();
            _tbY.Leave += (_, __) => ApplyPropsFromPanel();
            _tbW.Leave += (_, __) => ApplyPropsFromPanel();
            _tbH.Leave += (_, __) => ApplyPropsFromPanel();
            _numStrokeWidth.ValueChanged += (_, __) => ApplyPropsFromPanel();

            _propsPanel.Controls.Add(table);
            _propsPanel.Controls.Add(lblTitle);
            Controls.Add(_propsPanel);
            _propsPanel.BringToFront();
            MouseWheel += OnMouseWheel;
            _propsPanel.MouseWheel += OnMouseWheel;

            // События
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += OnKeyDown;

            SetTool(Tool.Select);
        }

        private void SetTool(Tool t)
        {
            _tool = t;
            _btnSelect.Checked = t == Tool.Select;
            _btnRect.Checked = t == Tool.Rect;
            _btnEll.Checked = t == Tool.Ellipse;
            _btnLine.Checked = t == Tool.Line;
            _status.Text = $"Инструмент: {_tool}" + (_stylePickMode ? "  |  Пипетка" : "");
        }

        // ===== Рендеринг =====
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Color.White);

            int topOffset = _ts.Height + MainMenuStrip!.Height;

            // Сначала настраиваем трансформацию:
            // 1) смещаем под меню и тулбар
            // 2) учитываем панорамирование
            // 3) учитываем масштаб
            g.TranslateTransform(_pan.X, _pan.Y + topOffset);
            g.ScaleTransform(_zoom, _zoom);

            // Рамка холста в "координатах документа"
            using (var pen = new Pen(Color.LightGray, 1 / _zoom)) // толщина не раздувается от масштаба
                g.DrawRectangle(pen, 0, 0, _canvasSize.Width, _canvasSize.Height);

            // Фигуры
            foreach (var s in _shapes)
                s.Draw(g);

            // Рамки выделения — тоже в координатах документа
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
                UpdatePanelFromSelection();
                if (_selection is not null)
                    _startBounds = _selection.Bounds;
                Invalidate();
            }
            else if (_tool == Tool.Rect)
            {
                var r = new RectShape(new RectangleF(pCanvas, SizeF.Empty));
                _shapes.Add(r);
                _selection = r;
                UpdatePanelFromSelection();
                _startBounds = r.Bounds;
            }
            else if (_tool == Tool.Ellipse)
            {
                var el = new EllipseShape(new RectangleF(pCanvas, SizeF.Empty));
                _shapes.Add(el);
                _selection = el;
                UpdatePanelFromSelection();
                _startBounds = el.Bounds;
            }
            else if (_tool == Tool.Line)
            {
                _newLine = new LineShape(pCanvas, pCanvas);
                _shapes.Add(_newLine);
                _selection = _newLine;
                UpdatePanelFromSelection();
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

                // Shift = квадрат
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    float dx = pCanvas.X - _dragStartCanvas.X;
                    float dy = pCanvas.Y - _dragStartCanvas.Y;

                    float size = MathF.Min(MathF.Abs(dx), MathF.Abs(dy));
                    float x = _dragStartCanvas.X;
                    float y = _dragStartCanvas.Y;

                    if (dx < 0) x -= size;
                    if (dy < 0) y -= size;

                    b = new RectangleF(x, y, size, size);
                }

                r.Bounds = b;
                Invalidate();
            }
            else if (_tool == Tool.Ellipse && _selection is EllipseShape el)
            {
                var b = SelectionRenderer.RectFromTwoPoints(_dragStartCanvas, pCanvas);

                // Shift = круг
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    float dx = pCanvas.X - _dragStartCanvas.X;
                    float dy = pCanvas.Y - _dragStartCanvas.Y;

                    float size = MathF.Min(MathF.Abs(dx), MathF.Abs(dy));
                    float x = _dragStartCanvas.X;
                    float y = _dragStartCanvas.Y;

                    if (dx < 0) x -= size;
                    if (dy < 0) y -= size;

                    b = new RectangleF(x, y, size, size);
                }

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
                    //TODO: При желании можно сделать отдельную команду которая использует перемещение фигуры при задании координат панель свойств
                    //var cmd = new MoveCommand(_selection, _netDelta);
                    //_history.Exec(cmd);
                }
            }
            else if (_tool == Tool.Rect || _tool == Tool.Ellipse)
            {
                if (_selection is not null)
                    _history.Exec(new AddShapeCommand(_shapes, _selection));

                SetTool(Tool.Select);
            }
            else if (_tool == Tool.Line)
            {
                if (_newLine is not null)
                {
                    _history.Exec(new AddShapeCommand(_shapes, _newLine));
                    _newLine = null;
                    SetTool(Tool.Select);
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
                    SetTool(Tool.Select);
                    return;
                }
                if (e.KeyCode == Keys.R)
                {
                    SetTool(Tool.Rect);
                    return;
                }
                if (e.KeyCode == Keys.E)
                {
                    SetTool(Tool.Ellipse);
                    return;
                }
                if (e.KeyCode == Keys.L)
                {
                    SetTool(Tool.Line);
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

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            // Масштаб только при зажатом Shift
            if ((ModifierKeys & Keys.Shift) != Keys.Shift)
                return;

            float factor = e.Delta > 0 ? 1.1f : 0.9f;

            // ограничиваем диапазон
            float newZoom = MathF.Max(0.1f, MathF.Min(4f, _zoom * factor));
            if (Math.Abs(newZoom - _zoom) < 0.0001f)
                return;

            int topOffset = _ts.Height + MainMenuStrip!.Height;

            // точка под курсором в координатах документа ДО зума
            var canvasBefore = new PointF(
                (e.X - _pan.X) / _zoom,
                (e.Y - topOffset - _pan.Y) / _zoom);

            _zoom = newZoom;

            // хотим, чтобы та же точка осталась под курсором ПОСЛЕ зума
            var screenAfterX = canvasBefore.X * _zoom + _pan.X;
            var screenAfterY = canvasBefore.Y * _zoom + _pan.Y + topOffset;

            _pan.X += e.X - screenAfterX;
            _pan.Y += (e.Y - topOffset) - (screenAfterY - topOffset);

            Invalidate();
        }

        private void DeleteSelection()
        {
            if (_selection is null) return;
            _history.Exec(new RemoveShapeCommand(_shapes, _selection));
            _selection = null;
            _multiSelection.Clear();
            UpdatePanelFromSelection();
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
            using var sfd = new SaveFileDialog() { Filter = "SVG файлы (*.svg)|*.svg" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var root = new XElement(SvgNs + "svg",
                    new XAttribute("version", "1.1"),
                    new XAttribute("width", F(_canvasSize.Width)),
                    new XAttribute("height", F(_canvasSize.Height)));

                foreach (var sh in _shapes)
                    SaveShapeToSvg(root, sh);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
                doc.Save(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Ошибка при сохранении SVG-файла:\r\n" + ex.Message,
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SaveShapeToSvg(XElement parent, IShape sh)
        {
            // локальный хелпер: превращаем цвет заливки в значение атрибута fill
            static string FillToSvg(Color c)
            {
                // "нет заливки" -> fill="none"
                if (c.A == 0) // полностью прозрачный
                    return "none";

                return ColorTranslator.ToHtml(c);
            }

            switch (sh)
            {
                case RectShape r:
                    parent.Add(new XElement(SvgNs + "rect",
                        new XAttribute("x", F(r.Bounds.X)),
                        new XAttribute("y", F(r.Bounds.Y)),
                        new XAttribute("width", F(r.Bounds.Width)),
                        new XAttribute("height", F(r.Bounds.Height)),
                        new XAttribute("fill", ColorTranslator.ToHtml(r.Fill)),
                        new XAttribute("stroke", ColorTranslator.ToHtml(r.Stroke)),
                        new XAttribute("stroke-width", F(r.StrokeWidth))));
                    break;

                case EllipseShape el:
                    var cx = el.Bounds.X + el.Bounds.Width / 2f;
                    var cy = el.Bounds.Y + el.Bounds.Height / 2f;
                    parent.Add(new XElement(SvgNs + "ellipse",
                        new XAttribute("cx", F(cx)),
                        new XAttribute("cy", F(cy)),
                        new XAttribute("rx", F(el.Bounds.Width / 2f)),
                        new XAttribute("ry", F(el.Bounds.Height / 2f)),
                        new XAttribute("fill", ColorTranslator.ToHtml(el.Fill)),
                        new XAttribute("stroke", ColorTranslator.ToHtml(el.Stroke)),
                        new XAttribute("stroke-width", F(el.StrokeWidth))));
                    break;

                case LineShape ln:
                    parent.Add(new XElement(SvgNs + "line",
                        new XAttribute("x1", F(ln.P1.X)),
                        new XAttribute("y1", F(ln.P1.Y)),
                        new XAttribute("x2", F(ln.P2.X)),
                        new XAttribute("y2", F(ln.P2.Y)),
                        new XAttribute("stroke", ColorTranslator.ToHtml(ln.Stroke)),
                        new XAttribute("stroke-width", F(ln.StrokeWidth))));
                    break;

                case GroupShape g:
                    // группы "плющим" в набор примитивов
                    foreach (var c in g.Children)
                        SaveShapeToSvg(parent, c);
                    break;
            }
        }

        private void DoOpen()
        {
            using var ofd = new OpenFileDialog() { Filter = "SVG файлы (*.svg)|*.svg" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var doc = XDocument.Load(ofd.FileName);
                var root = doc.Root;
                if (root == null || root.Name.LocalName != "svg")
                {
                    MessageBox.Show(
                        this,
                        "Файл не является корректным SVG-документом.",
                        "Ошибка открытия",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _shapes.Clear();

                // размер холста: пытаемся вытащить width/height, иначе viewBox
                float w = _canvasSize.Width;
                float h = _canvasSize.Height;

                var wAttr = root.Attribute("width");
                var hAttr = root.Attribute("height");
                if (wAttr != null && hAttr != null)
                {
                    w = ParseSvgLength(wAttr.Value, w);
                    h = ParseSvgLength(hAttr.Value, h);
                }
                else
                {
                    var vbAttr = root.Attribute("viewBox");
                    if (vbAttr != null)
                    {
                        var parts = vbAttr.Value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 4 &&
                            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var vw) &&
                            float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var vh))
                        {
                            w = vw;
                            h = vh;
                        }
                    }
                }

                _canvasSize = new SizeF(w, h);

                foreach (var el in root.Descendants())
                {
                    switch (el.Name.LocalName)
                    {
                        case "rect":
                            {
                                var rx = ParseSvgLength(el.Attribute("x")?.Value, 0);
                                var ry = ParseSvgLength(el.Attribute("y")?.Value, 0);
                                var rw = ParseSvgLength(el.Attribute("width")?.Value, 0);
                                var rh = ParseSvgLength(el.Attribute("height")?.Value, 0);
                                if (rw <= 0 || rh <= 0) break;

                                var r = new RectShape(new RectangleF(rx, ry, rw, rh))
                                {
                                    Fill = ParseColor(el, "fill", Color.Transparent),
                                    Stroke = ParseColor(el, "stroke", Color.Black),
                                    StrokeWidth = ParseSvgLength(el.Attribute("stroke-width")?.Value, 1f)
                                };
                                _shapes.Add(r);
                                break;
                            }

                        case "ellipse":
                            {
                                var cx = ParseSvgLength(el.Attribute("cx")?.Value, 0);
                                var cy = ParseSvgLength(el.Attribute("cy")?.Value, 0);
                                var rx2 = ParseSvgLength(el.Attribute("rx")?.Value, 0);
                                var ry2 = ParseSvgLength(el.Attribute("ry")?.Value, 0);
                                if (rx2 <= 0 || ry2 <= 0) break;

                                var eShape = new EllipseShape(new RectangleF(cx - rx2, cy - ry2, rx2 * 2, ry2 * 2))
                                {
                                    Fill = ParseColor(el, "fill", Color.Transparent),
                                    Stroke = ParseColor(el, "stroke", Color.Black),
                                    StrokeWidth = ParseSvgLength(el.Attribute("stroke-width")?.Value, 1f)
                                };
                                _shapes.Add(eShape);
                                break;
                            }

                        case "line":
                            {
                                var x1 = ParseSvgLength(el.Attribute("x1")?.Value, 0);
                                var y1 = ParseSvgLength(el.Attribute("y1")?.Value, 0);
                                var x2 = ParseSvgLength(el.Attribute("x2")?.Value, 0);
                                var y2 = ParseSvgLength(el.Attribute("y2")?.Value, 0);


                                var l = new LineShape(new PointF(x1, y1), new PointF(x2, y2))
                                {
                                    Stroke = ParseColor(el, "stroke", Color.Black),
                                    StrokeWidth = ParseSvgLength(el.Attribute("stroke-width")?.Value, 1f)
                                };
                                _shapes.Add(l);
                                break;
                            }
                    }
                }

                _selection = null;
                _multiSelection.Clear();
                UpdatePanelFromSelection();
                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Ошибка при открытии SVG-файла:\r\n" + ex.Message,
                    "Ошибка открытия",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DoNewCanvas()
        {
            using var dlg = new NewCanvasForm(_canvasSize);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _canvasSize = dlg.CanvasSize;
            _shapes.Clear();
            _selection = null;
            _multiSelection.Clear();
            UpdatePanelFromSelection();
            Invalidate();
        }

        private static float ParseF(XElement el, string name, float def = 0f)
        {
            var a = el.Attribute(name);
            if (a == null) return def;

            var s = a.Value.Trim();

            // часто бывает "800px"
            if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                s = s[..^2].Trim();

            // некоторые SVG пишут числа в экспоненциальной форме
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v;

            return def;
        }

        private static float ParseSvgLength(string? raw, float def = 0f)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return def;

            var s = raw.Trim();

            if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                s = s[..^2].Trim();

            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v;

            return def;
        }

        private static Color ParseColor(XElement el, string name, Color def)
        {
            var a = el.Attribute(name);
            if (a == null)
                return def;

            var s = a.Value.Trim();

            // SVG-стандартное "нет заливки"
            if (string.Equals(s, "none", StringComparison.OrdinalIgnoreCase))
                return Color.Transparent;

            // обрабатываем "transparent"
            if (string.Equals(s, "transparent", StringComparison.OrdinalIgnoreCase))
                return Color.Transparent;

            try
            {
                return ColorTranslator.FromHtml(s);
            }
            catch
            {
                // если вдруг прилетело что-то странное - не падаем, а используем дефолт
                return def;
            }
        }

        private void UpdatePanelFromSelection()
        {
            if (_updatingPropsFromSelection) return;
            _updatingPropsFromSelection = true;

            if (_selection is null)
            {
                _tbX.Text = "";
                _tbY.Text = "";
                _tbW.Text = "";
                _tbH.Text = "";
                _btnFill.BackColor = SystemColors.Control;
                _btnStroke.BackColor = SystemColors.Control;
                _numStrokeWidth.Value = 0;
            }
            else
            {
                var b = _selection.Bounds;
                _tbX.Text = b.X.ToString("0.##");
                _tbY.Text = b.Y.ToString("0.##");
                _tbW.Text = b.Width.ToString("0.##");
                _tbH.Text = b.Height.ToString("0.##");
                _btnFill.BackColor = _selection.Fill.IsEmpty ? SystemColors.Control : _selection.Fill;
                _btnStroke.BackColor = _selection.Stroke.IsEmpty ? SystemColors.Control : _selection.Stroke;
                _numStrokeWidth.Value = (decimal)_selection.StrokeWidth;
            }

            _updatingPropsFromSelection = false;
        }

        private void ApplyPropsFromPanel()
        {
            if (_updatingPropsFromSelection) return;
            if (_selection is null) return;

            if (!float.TryParse(_tbX.Text, out var x)) x = _selection.Bounds.X;
            if (!float.TryParse(_tbY.Text, out var y)) y = _selection.Bounds.Y;
            if (!float.TryParse(_tbW.Text, out var w)) w = _selection.Bounds.Width;
            if (!float.TryParse(_tbH.Text, out var h)) h = _selection.Bounds.Height;

            var oldBounds = _selection.Bounds;
            var newBounds = new RectangleF(x, y, w, h);

            var oldFill = _selection.Fill;
            var oldStroke = _selection.Stroke;
            var oldWidth = _selection.StrokeWidth;

            var newFill = _selection.Fill;
            var newStroke = _selection.Stroke;
            var newWidth = (float)_numStrokeWidth.Value;

            if (newBounds == oldBounds && newFill == oldFill && newStroke == oldStroke && Math.Abs(newWidth - oldWidth) < 0.001f)
                return;

            var cmd = new PropertyChangeCommand(
                _selection,
                oldBounds, newBounds,
                oldFill, newFill,
                oldStroke, newStroke,
                oldWidth, newWidth);

            _history.Exec(cmd);
            Invalidate();
        }

        private void ChangeColor(bool fill)
        {
            if (_selection is null) return;
            _colorDialog.Color = fill ? _selection.Fill : _selection.Stroke;
            if (_colorDialog.ShowDialog() != DialogResult.OK) return;

            var oldBounds = _selection.Bounds;
            var newBounds = oldBounds;

            var oldFill = _selection.Fill;
            var oldStroke = _selection.Stroke;
            var oldWidth = _selection.StrokeWidth;

            var newFill = fill ? _colorDialog.Color : oldFill;
            var newStroke = fill ? oldStroke : _colorDialog.Color;
            var newWidth = oldWidth;

            var cmd = new PropertyChangeCommand(
                _selection,
                oldBounds, newBounds,
                oldFill, newFill,
                oldStroke, newStroke,
                oldWidth, newWidth);

            _history.Exec(cmd);
            UpdatePanelFromSelection();
            Invalidate();
        }

        private static string F(float v) =>
        v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
