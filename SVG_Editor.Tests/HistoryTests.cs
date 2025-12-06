using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor.Commands;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// Тесты стека истории команд: Exec, Undo и Redo.
    /// </summary>
    [TestClass]
    public class HistoryTests
    {
        private sealed class DummyCommand : ICommand
        {
            public int Counter { get; private set; }

            public void Do() => Counter++;
            public void Undo() => Counter--;
        }

        /// <summary>
        /// Exec должен вызвать Do у команды и сохранить её в истории.
        /// </summary>
        [TestMethod]
        public void Exec_StoresCommandAndCallsDo()
        {
            var history = new History();
            var cmd = new DummyCommand();

            history.Exec(cmd);

            Assert.AreEqual(1, cmd.Counter);
        }

        /// <summary>
        /// Undo должен откатить последнюю команду,
        /// если она была выполнена.
        /// </summary>
        [TestMethod]
        public void Undo_RevertsLastCommand()
        {
            var history = new History();
            var cmd = new DummyCommand();

            history.Exec(cmd);
            history.Undo();

            Assert.AreEqual(0, cmd.Counter);
        }

        /// <summary>
        /// Redo должен повторно выполнить отменённую команду.
        /// </summary>
        [TestMethod]
        public void Redo_ReexecutesLastUndoneCommand()
        {
            var history = new History();
            var cmd = new DummyCommand();

            history.Exec(cmd);
            history.Undo();
            history.Redo();

            Assert.AreEqual(1, cmd.Counter);
        }
    }
}