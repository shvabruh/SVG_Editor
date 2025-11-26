using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    public sealed class AddShapeCommand : ICommand
    {
        private readonly List<IShape> _list;
        private readonly IShape _s;
        public AddShapeCommand(List<IShape> list, IShape s)
        { _list = list; _s = s; }

        public void Do()
        {
            if (!_list.Contains(_s))
                _list.Add(_s);
        }

        public void Undo() => _list.Remove(_s);
    }
}
