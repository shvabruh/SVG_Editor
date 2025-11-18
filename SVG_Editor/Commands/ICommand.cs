using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    public interface ICommand
    {
        void Do();
        void Undo();
    }
}
