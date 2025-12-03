using SVG_Editor.Shapes;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Обобщённая команда изменения свойств фигуры:
    /// границ, заливки, обводки и толщины.
    /// Используется панелью свойств.
    public sealed class PropertyChangeCommand: ICommand
    {
        private readonly IShape _shape;
        private readonly RectangleF _oldBounds, _newBounds;
        private readonly Color _oldFill, _newFill;
        private readonly Color _oldStroke, _newStroke;
        private readonly float _oldWidth, _newWidth;

        public PropertyChangeCommand(
            IShape shape,
            RectangleF oldBounds, RectangleF newBounds,
            Color oldFill, Color newFill,
            Color oldStroke, Color newStroke,
            float oldWidth, float newWidth)
        {
            _shape = shape;
            _oldBounds = oldBounds;
            _newBounds = newBounds;
            _oldFill = oldFill;
            _newFill = newFill;
            _oldStroke = oldStroke;
            _newStroke = newStroke;
            _oldWidth = oldWidth;
            _newWidth = newWidth;
        }

        public void Do()
        {
            _shape.Bounds = _newBounds;
            _shape.Fill = _newFill;
            _shape.Stroke = _newStroke;
            _shape.StrokeWidth = _newWidth;
        }

        public void Undo()
        {
            _shape.Bounds = _oldBounds;
            _shape.Fill = _oldFill;
            _shape.Stroke = _oldStroke;
            _shape.StrokeWidth = _oldWidth;
        }
    }
}
