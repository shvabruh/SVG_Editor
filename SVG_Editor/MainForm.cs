using SVG_Editor.Enums;
using SVG_Editor.Shapes;
using System.Reflection.Metadata;

namespace SVG_Editor
{
    public sealed partial class MainForm : Form
    {
        private readonly List<IShape> _shapes = new();
        private readonly History _history = new();
        private Tool _tool = Tool.Select;
        private IShape? _selection;
        private IShape? _clipboard;
        private HandleKind _activeHandle = HandleKind.None;
        private PointF _dragStartCanvas; // start point in canvas coords
        private RectangleF _startBounds; // selection bounds at drag start
        private PointF _netDelta; // accumulate delta for single Move command
        private LineShape? _newLine; // during line drawing
        private SizeF _canvasSize = new(1200, 800);


        // Viewport (optional zoom/pan; kept simple here)
        private float _zoom = 1f;
        private PointF _pan = new(0, 0);


        private readonly ToolStrip _ts = new();
        private readonly StatusStrip _ss = new();
        private readonly ToolStripStatusLabel _status = new();

        public MainForm()
        {
            Text = "SVG Editor — Minimal";
            DoubleBuffered = true;
            ClientSize = new Size(1000, 700);
            KeyPreview = true;


            // Toolbar
            var bSelect = new ToolStripButton("Select") { Checked = true, CheckOnClick = true };
            var bRect = new ToolStripButton("Rect") { CheckOnClick = true };
            var bEll = new ToolStripButton("Ellipse") { CheckOnClick = true };
            var bLine = new ToolStripButton("Line") { CheckOnClick = true };
            bSelect.Click += (_,) => SetTool(Tool.Select, bSelect, bRect, bEll, bLine);
            bRect.Click += (_,) => SetTool(Tool.Rect, bSelect, bRect, bEll, bLine);
            bEll.Click += (_,) => SetTool(Tool.Ellipse, bSelect, bRect, bEll, bLine);
            bLine.Click += (_,) => SetTool(Tool.Line, bSelect, bRect, bEll, bLine);


            var bOpen = new ToolStripButton("Open"); bOpen.Click += (_,) => DoOpen();
            var bSave = new ToolStripButton("Save"); bSave.Click += (_,) => DoSave();
            var bUndo = new ToolStripButton("Undo"); bUndo.Click += (_,) => { _history.Undo(); Invalidate(); };
            var bRedo = new ToolStripButton("Redo"); bRedo.Click += (_,) => { _history.Redo(); Invalidate(); };
            var bDel = new ToolStripButton("Delete"); bDel.Click += (_,) => DeleteSelection();


            _ts.Items.AddRange(new ToolStripItem[] { bOpen, bSave, new ToolStripSeparator(), bUndo, bRedo, new ToolStripSeparator(), bSelect, bRect, bEll, bLine, new ToolStripSeparator(), bDel });
            _ts.GripStyle = ToolStripGripStyle.Hidden;
            Controls.Add(_ts);


            // Status bar
            _ss.Items.Add(_status);
            Controls.Add(_ss);


            // Shortcuts
            var miOpen = new ToolStripMenuItem("Open", null, (_,) => DoOpen()) { ShortcutKeys = Keys.Control | Keys.O };
            var miSave = new ToolStripMenuItem("Save", null, (_,) => DoSave()) { ShortcutKeys = Keys.Control | Keys.S };
            var miUndo = new ToolStripMenuItem("Undo", null, (_,) => { _history.Undo(); Invalidate(); }) { ShortcutKeys = Keys.Control | Keys.Z };
            var miRedo = new ToolStripMenuItem("Redo", null, (_,) => { _history.Redo(); Invalidate(); }) { ShortcutKeys = Keys.Control | Keys.Y };
            var miDel = new ToolStripMenuItem("Delete", null, (_,) => DeleteSelection()) { ShortcutKeys = Keys.Delete };
            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("File"); file.DropDownItems.AddRange(new[] { miOpen, miSave });
            var edit = new ToolStripMenuItem("Edit"); edit.DropDownItems.AddRange(new[] { miUndo, miRedo, miDel });
            menu.Items.AddRange(new[] { file, edit });
            MainMenuStrip = menu; Controls.Add(menu);


            // Mouse & keyboard
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += OnKeyDown;
        }
    }
}
