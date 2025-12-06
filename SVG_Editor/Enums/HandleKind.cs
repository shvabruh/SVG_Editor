using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Enums
{
    /// <summary>
    /// Типы ручек (углы и стороны) рамки выделения, используемые для изменения размера фигур.
    /// </summary>
    public enum HandleKind
    {
        None,
        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        NW
    }
}
