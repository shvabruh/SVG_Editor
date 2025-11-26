using SVG_Editor.Shapes;


namespace SVG_Editor.Commands
{
    public sealed class MoveCommand : ICommand
    {
        private readonly IShape _s;
        private readonly PointF _d;
        public MoveCommand(IShape s, PointF d) { _s = s; _d = d; }

        public void Do() => _s.MoveBy(_d);
        public void Undo() => _s.MoveBy(new PointF(-_d.X, -_d.Y));
    }
}
