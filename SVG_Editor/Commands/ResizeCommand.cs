using SVG_Editor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    public sealed class ResizeCommand : ICommand
    {
        private readonly IShape _s;
        private readonly RectangleF _from, _to;
        public ResizeCommand(IShape s, RectangleF from, RectangleF to)
        { _s = s; _from = from; _to = to; }

        public void Do() => _s.Bounds = _to;
        public void Undo() => _s.Bounds = _from;
    }
}
