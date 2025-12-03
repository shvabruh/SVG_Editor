using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда добавления новой фигуры в коллекцию фигур на холсте.
    /// Поддерживает отмену (удаление добавленной фигуры).
    /// </summary>
    public sealed class AddShapeCommand : ICommand
    {
        private readonly List<IShape> _list;
        private readonly IShape _s;
        public AddShapeCommand(List<IShape> list, IShape s)
        { _list = list; _s = s; }

        /// <summary>
        /// Добавляет фигуру в коллекцию.
        /// </summary>
        public void Do()
        {
            if (!_list.Contains(_s))
                _list.Add(_s);
        }

        /// <summary>
        /// Удаляет ранее добавленную фигуру из коллекции.
        /// </summary>
        public void Undo() => _list.Remove(_s);
    }
}
