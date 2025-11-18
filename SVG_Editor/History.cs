using SVG_Editor.Commands;



namespace SVG_Editor
{
    public sealed class History
    {
        private readonly Stack<ICommand> _undo = new();
        private readonly Stack<ICommand> _redo = new();

        public void Exec(ICommand c)
        {
            c.Do();
            _undo.Push(c);
            _redo.Clear();
        }

        public void Undo()
        {
            if (_undo.Count <= 0) return;
            var c = _undo.Pop();
            c.Undo();
            _redo.Push(c);
        }

        public void Redo()
        {
            if (_redo.Count <= 0) return;
            var c = _redo.Pop();
            c.Do();
            _undo.Push(c);
        }
    }
}
