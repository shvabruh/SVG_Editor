using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Commands
{
    /// <summary>
    /// Базовый интерфейс команды изменения документа,
    /// поддерживающей выполнение и отмену (паттерн Command).
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Выполняет действие команды и изменяет состояние документа.
        /// </summary>
        void Do();

        /// <summary>
        /// Отменяет ранее выполненное действие и возвращает состояние назад.
        /// </summary>
        void Undo();
    }
}
