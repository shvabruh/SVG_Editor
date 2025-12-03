using SVG_Editor.Commands;

namespace SVG_Editor
{
    /// <summary>
    /// Стек истории изменений документа.
    /// Хранит выполненные команды и позволяет выполнять операции Undo/Redo.
    /// </summary>
    public sealed class History
    {
        private readonly Stack<ICommand> _undo = new();
        private readonly Stack<ICommand> _redo = new();

        /// <summary>
        /// Выполняет команду, добавляет её в стек и очищает «ветку Redo».  
        /// Используется при любых новых действиях пользователя.
        /// </summary>
        public void Exec(ICommand c)
        {
            c.Do();
            _undo.Push(c);

 
            // Очищает историю, удаляя все команды.
            _redo.Clear();
        }

        /// <summary>
        /// Отменяет последнюю выполненную команду, если она есть.
        /// </summary>
        public void Undo()
        {
            if (_undo.Count <= 0) return;
            var c = _undo.Pop();
            c.Undo();
            _redo.Push(c);
        }

        /// <summary>
        /// Повторно выполняет последнюю отменённую команду, если она есть.
        /// </summary>
        public void Redo()
        {
            if (_redo.Count <= 0) return;
            var c = _redo.Pop();
            c.Do();
            _undo.Push(c);
        }
    }
}
