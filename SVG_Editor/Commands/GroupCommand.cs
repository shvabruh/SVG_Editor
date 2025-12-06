using SVG_Editor.Shapes;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Команда группировки нескольких фигур в единый объект GroupShape.
    /// При Undo возвращает фигуры в исходный список без группы.
    /// </summary>
    public sealed class GroupCommand: ICommand
    {
        private readonly List<IShape> _list;
        private readonly List<IShape> _children;
        private readonly GroupShape _group;
        private readonly int _insertIndex;

        public GroupShape Group => _group;

        public GroupCommand(List<IShape> list, IEnumerable<IShape> shapesToGroup)
        {
            _list = list;
            _children = shapesToGroup.Distinct().ToList();
            if (_children.Count < 2)
                throw new InvalidOperationException("Нужно минимум 2 фигуры для группировки");

            _insertIndex = _children
                .Select(c => list.IndexOf(c))
                .Where(i => i >= 0)
                .DefaultIfEmpty(-1)
                .Min();

            if (_insertIndex < 0)
                throw new InvalidOperationException("Некоторые фигуры для группировки не найдены в списке");

            _group = new GroupShape(_children);
        }

        /// <summary>
        /// Добавляет фигуру в коллекцию.
        /// </summary>
        public void Do()
        {
            foreach (var c in _children)
                _list.Remove(c);

            _list.Insert(_insertIndex, _group);
        }

        /// <summary>
        /// Удаляет ранее добавленную фигуру из коллекции.
        /// </summary>
        public void Undo()
        {
            _list.Remove(_group);
            int idx = _insertIndex;
            foreach (var c in _children)
            {
                _list.Insert(idx, c);
                idx++;
            }
        }
    }
}
