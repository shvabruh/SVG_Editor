using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor.Commands;
using SVG_Editor.Shapes;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// “есты команд редактировани€ фигур и их коллекций.
    /// </summary>
    [TestClass]
    public class CommandTests
    {
        /// <summary>
        ///  оманда добавлени€ должна помещать фигуру в список,
        /// а Undo Ч удал€ть еЄ.
        /// </summary>
        [TestMethod]
        public void AddShapeCommand_AddsShape_AndUndoRemoves()
        {
            var list = new List<IShape>();
            var rect = new RectShape(new RectangleF(0, 0, 10, 10));
            var cmd = new AddShapeCommand(list, rect);

            cmd.Do();
            Assert.AreEqual(1, list.Count);
            Assert.AreSame(rect, list[0]);

            cmd.Undo();
            Assert.AreEqual(0, list.Count);
        }

        /// <summary>
        ///  оманда удалени€ должна вынимать фигуру из списка
        /// и уметь восстанавливать еЄ обратно.
        /// </summary>
        [TestMethod]
        public void RemoveShapeCommand_RemovesShape_AndUndoRestores()
        {
            var rect = new RectShape(new RectangleF(0, 0, 10, 10));
            var list = new List<IShape> { rect };
            var cmd = new RemoveShapeCommand(list, rect);

            cmd.Do();
            Assert.AreEqual(0, list.Count);

            cmd.Undo();
            Assert.AreEqual(1, list.Count);
            Assert.AreSame(rect, list[0]);
        }

        /// <summary>
        ///  оманда Move должна смещать фигуру на заданный вектор
        /// и уметь возвращать еЄ в исходное положение.
        /// </summary>
        [TestMethod]
        public void MoveCommand_MovesShape_AndUndoReturnsBack()
        {
            var rect = new RectShape(new RectangleF(10, 20, 30, 40));
            var cmd = new MoveCommand(rect, new PointF(5, -3));

            cmd.Do();
            Assert.AreEqual(15, rect.Bounds.X);
            Assert.AreEqual(17, rect.Bounds.Y);

            cmd.Undo();
            Assert.AreEqual(10, rect.Bounds.X);
            Assert.AreEqual(20, rect.Bounds.Y);
        }

        /// <summary>
        ///  оманда Resize должна мен€ть Bounds фигуры
        /// и корректно откатыватьс€.
        /// </summary>
        [TestMethod]
        public void ResizeCommand_ChangesBounds_AndUndoRestores()
        {
            var rect = new RectShape(new RectangleF(0, 0, 10, 10));
            var oldBounds = rect.Bounds;
            var newBounds = new RectangleF(0, 0, 20, 5);

            var cmd = new ResizeCommand(rect, oldBounds, newBounds);

            cmd.Do();
            Assert.AreEqual(newBounds, rect.Bounds);

            cmd.Undo();
            Assert.AreEqual(oldBounds, rect.Bounds);
        }

        /// <summary>
        ///  оманда ApplyStyle должна мен€ть стиль фигуры
        /// и уметь вернуть прежние значени€.
        /// </summary>
        [TestMethod]
        public void ApplyStyleCommand_ChangesStyle_AndUndoRestores()
        {
            var rect = new RectShape(new RectangleF(0, 0, 10, 10))
            {
                Fill = Color.Red,
                Stroke = Color.Blue,
                StrokeWidth = 1
            };

            var cmd = new ApplyStyleCommand(rect, Color.Green, Color.Black, 3);

            cmd.Do();
            Assert.AreEqual(Color.Green, rect.Fill);
            Assert.AreEqual(Color.Black, rect.Stroke);
            Assert.AreEqual(3, rect.StrokeWidth);

            cmd.Undo();
            Assert.AreEqual(Color.Red, rect.Fill);
            Assert.AreEqual(Color.Blue, rect.Stroke);
            Assert.AreEqual(1, rect.StrokeWidth);
        }


        /// <summary>
        ///  оманда Group должна замен€ть фигуры на одну GroupShape
        /// и восстанавливать исходный список при Undo.
        /// </summary>
        [TestMethod]
        public void GroupCommand_GroupsShapes_AndUndoUngroups()
        {
            var r1 = new RectShape(new RectangleF(0, 0, 10, 10));
            var r2 = new RectShape(new RectangleF(20, 0, 10, 10));
            var list = new List<IShape> { r1, r2 };

            var cmd = new GroupCommand(list, new[] { r1, r2 });

            cmd.Do();

            Assert.AreEqual(1, list.Count);
            Assert.IsInstanceOfType(list[0], typeof(GroupShape));
            var group = (GroupShape)list[0];
            Assert.AreEqual(2, group.Children.Count);

            cmd.Undo();

            Assert.AreEqual(2, list.Count);
            CollectionAssert.Contains(list, r1);
            CollectionAssert.Contains(list, r2);
        }

        /// <summary>
        ///  оманда Ungroup должна разбивать группу на отдельные фигуры
        /// и уметь восстановить исходную группу.
        /// </summary>
        [TestMethod]
        public void UngroupCommand_SplitsGroup_AndUndoRestoresGroup()
        {
            var r1 = new RectShape(new RectangleF(0, 0, 10, 10));
            var r2 = new RectShape(new RectangleF(20, 0, 10, 10));
            var group = new GroupShape(new[] { r1, r2 });

            var list = new List<IShape> { group };
            var cmd = new UngroupCommand(list, group);

            cmd.Do();

            Assert.AreEqual(2, list.Count);
            CollectionAssert.Contains(list, r1);
            CollectionAssert.Contains(list, r2);

            cmd.Undo();

            Assert.AreEqual(1, list.Count);
            Assert.AreSame(group, list[0]);
        }
    }
}