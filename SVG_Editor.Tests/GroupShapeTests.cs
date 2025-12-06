using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor.Shapes;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// Тесты групповой фигуры: границы, перемещение и hit-test по дочерним фигурам.
    /// </summary>
    [TestClass]
    public class GroupShapeTests
    {
        /// <summary>
        /// После построения группы её Bounds должны охватывать
        /// все дочерние фигуры.
        /// </summary>
        [TestMethod]
        public void GroupShape_RecalcBounds_UsesChildrenBounds()
        {
            var r = new RectShape(new RectangleF(10, 10, 20, 20));   // (10,10)-(30,30)
            var e = new EllipseShape(new RectangleF(40, 5, 10, 10)); // (40,5)-(50,15)

            var group = new GroupShape(new IShape[] { r, e });

            Assert.AreEqual(10, group.Bounds.X);
            Assert.AreEqual(5, group.Bounds.Y);
            Assert.AreEqual(50 - 10, group.Bounds.Width);
            Assert.AreEqual(30 - 5, group.Bounds.Height);
        }

        /// <summary>
        /// Перемещение группы должно перемещать и Bounds группы,
        /// и все дочерние фигуры.
        /// </summary>
        [TestMethod]
        public void GroupShape_MoveBy_MovesAllChildrenAndBounds()
        {
            var r = new RectShape(new RectangleF(0, 0, 10, 10));
            var e = new EllipseShape(new RectangleF(20, 0, 10, 10));

            var group = new GroupShape(new IShape[] { r, e });

            var beforeR = r.Bounds;
            var beforeE = e.Bounds;
            var beforeG = group.Bounds;

            group.MoveBy(new PointF(5, 3));

            Assert.AreEqual(beforeR.X + 5, r.Bounds.X);
            Assert.AreEqual(beforeR.Y + 3, r.Bounds.Y);
            Assert.AreEqual(beforeE.X + 5, e.Bounds.X);
            Assert.AreEqual(beforeE.Y + 3, e.Bounds.Y);
            Assert.AreEqual(beforeG.X + 5, group.Bounds.X);
            Assert.AreEqual(beforeG.Y + 3, group.Bounds.Y);
        }

        /// <summary>
        /// HitTest группы должен срабатывать, если точка попадает
        /// хотя бы в одну из дочерних фигур.
        /// </summary>
        [TestMethod]
        public void GroupShape_HitTest_WhenPointInsideChild_ReturnsTrue()
        {
            var r = new RectShape(new RectangleF(0, 0, 10, 10));
            var e = new EllipseShape(new RectangleF(20, 0, 10, 10));
            var group = new GroupShape(new IShape[] { r, e });

            Assert.IsTrue(group.HitTest(new PointF(5, 5)));   // внутри прямоугольника
            Assert.IsTrue(group.HitTest(new PointF(25, 5)));  // внутри эллипса
            Assert.IsFalse(group.HitTest(new PointF(50, 50)));
        }
    }
}