using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor.Commands;
using SVG_Editor.Shapes;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// “есты команды изменени€ свойств фигуры,
    /// используемой панелью свойств редактора.
    /// </summary>
    [TestClass]
    public class PropertyChangeCommandTests
    {
        /// <summary>
        ///  оманда должна одновременно мен€ть Bounds, заливку, обводку и толщину,
        /// а Undo Ч полностью восстанавливать старые значени€.
        /// </summary>
        [TestMethod]
        public void PropertyChangeCommand_ChangesAllProperties_AndUndoRestores()
        {
            var rect = new RectShape(new RectangleF(0, 0, 10, 10))
            {
                Fill = Color.Red,
                Stroke = Color.Blue,
                StrokeWidth = 1
            };

            var oldBounds = rect.Bounds;
            var newBounds = new RectangleF(5, 5, 20, 30);

            var cmd = new PropertyChangeCommand(
                rect,
                oldBounds, newBounds,
                rect.Fill, Color.Green,
                rect.Stroke, Color.Black,
                rect.StrokeWidth, 2);

            cmd.Do();

            Assert.AreEqual(newBounds, rect.Bounds);
            Assert.AreEqual(Color.Green, rect.Fill);
            Assert.AreEqual(Color.Black, rect.Stroke);
            Assert.AreEqual(2, rect.StrokeWidth);

            cmd.Undo();

            Assert.AreEqual(oldBounds, rect.Bounds);
            Assert.AreEqual(Color.Red, rect.Fill);
            Assert.AreEqual(Color.Blue, rect.Stroke);
            Assert.AreEqual(1, rect.StrokeWidth);
        }
    }
}