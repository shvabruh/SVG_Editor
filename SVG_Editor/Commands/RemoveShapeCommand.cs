using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда удаления фигуры с холста.
    /// Позволяет вернуть фигуру обратно при Undo.
    /// </summary>
    public sealed class RemoveShapeCommand : ICommand
    {
        private readonly List<IShape> _list;
        private readonly IShape _s;
        private int _idx;

        public RemoveShapeCommand(List<IShape> list, IShape s)
        { _list = list; _s = s; }

        /// <summary>
        /// Добавляет фигуру в коллекцию.
        /// </summary>
        public void Do()
        {
            _idx = _list.IndexOf(_s);
            if (_idx >= 0)
                _list.RemoveAt(_idx);
        }

        /// <summary>
        /// Удаляет ранее добавленную фигуру из коллекции.
        /// </summary>
        public void Undo()
        {
            if (_idx < 0 || _idx > _list.Count)
                _list.Add(_s);
            else
                _list.Insert(_idx, _s);
        }
    }
}
