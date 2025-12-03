using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда изменения размеров фигуры.
    /// Сохраняет старые и новые границы, чтобы поддерживать Undo/Redo.
    /// </summary>
    public sealed class ResizeCommand : ICommand
    {
        private readonly IShape _s;
        private readonly RectangleF _from, _to;
        public ResizeCommand(IShape s, RectangleF from, RectangleF to)
        { _s = s; _from = from; _to = to; }

        /// <summary>
        /// Добавляет фигуру в коллекцию.
        /// </summary>
        public void Do() => _s.Bounds = _to;

        /// <summary>
        /// Удаляет ранее добавленную фигуру из коллекции.
        /// </summary>
        public void Undo() => _s.Bounds = _from;
    }
}
