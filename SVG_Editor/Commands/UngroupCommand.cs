using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда разгруппировки объекта GroupShape обратно в отдельные фигуры.
    /// </summary>
    public sealed class UngroupCommand: ICommand
    {
        private readonly List<IShape> _list;
        private readonly GroupShape _group;
        private readonly int _index;

        public UngroupCommand(List<IShape> list, GroupShape group)
        {
            _list = list;
            _group = group;
            _index = list.IndexOf(group);
        }

        public void Do()
        {
            if (_index < 0) return;

            int idx = _list.IndexOf(_group);
            if (idx < 0) return;

            _list.RemoveAt(idx);
            int insert = idx;
            foreach (var c in _group.Children)
            {
                _list.Insert(insert, c);
                insert++;
            }
        }

        public void Undo()
        {
            foreach (var c in _group.Children)
                _list.Remove(c);

            int idx = _index >= 0 && _index <= _list.Count ? _index : _list.Count;
            if (!_list.Contains(_group))
                _list.Insert(idx, _group);
        }
    }
}
