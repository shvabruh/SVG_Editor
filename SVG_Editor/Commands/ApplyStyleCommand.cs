using SVG_Editor.Shapes;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда изменения стиля фигуры (цвет заливки, обводки и толщина).
    /// Поддерживает полное восстановление предыдущих значений.
    /// </summary>
    public sealed class ApplyStyleCommand : ICommand
    {
        private readonly IShape _shape;
        private readonly Color _oldFill, _newFill;
        private readonly Color _oldStroke, _newStroke;
        private readonly float _oldWidth, _newWidth;

        public ApplyStyleCommand(IShape s, Color fill, Color stroke, float width)
        {
            _shape = s;
            _oldFill = s.Fill;
            _oldStroke = s.Stroke;
            _oldWidth = s.StrokeWidth;

            _newFill = fill;
            _newStroke = stroke;
            _newWidth = width;
        }

        /// <summary>
        /// Добавляет фигуру в коллекцию.
        /// </summary>
        public void Do()
        {
            _shape.Fill = _newFill;
            _shape.Stroke = _newStroke;
            _shape.StrokeWidth = _newWidth;
        }

        /// <summary>
        /// Удаляет ранее добавленную фигуру из коллекции.
        /// </summary>
        public void Undo()
        {
            _shape.Fill = _oldFill;
            _shape.Stroke = _oldStroke;
            _shape.StrokeWidth = _oldWidth;
        }
    }
}
