using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor.Shapes;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// Тесты базового поведения простых фигур:
    /// перемещение, клонирование и hit-test.
    /// </summary>
    [TestClass]
    public class ShapeTests
    {
        /// <summary>
        /// Перемещение прямоугольника должно корректно сдвигать Bounds.
        /// </summary>
        [TestMethod]
        public void RectShape_MoveBy_ChangesBounds()
        {
            var r = new RectShape(new RectangleF(10, 20, 30, 40));

            r.MoveBy(new PointF(5, -10));

            Assert.AreEqual(15, r.Bounds.X);
            Assert.AreEqual(10, r.Bounds.Y);
            Assert.AreEqual(30, r.Bounds.Width);
            Assert.AreEqual(40, r.Bounds.Height);
        }

        /// <summary>
        /// Клон эллипса должен копировать все параметры,
        /// но быть независимым объектом.
        /// </summary>
        [TestMethod]
        public void EllipseShape_Clone_ReturnsIndependentCopy()
        {
            var e = new EllipseShape(new RectangleF(0, 0, 50, 60))
            {
                Fill = Color.Red,
                Stroke = Color.Blue,
                StrokeWidth = 2
            };

            var copy = (EllipseShape)e.Clone();

            Assert.AreEqual(e.Bounds, copy.Bounds);
            Assert.AreEqual(e.Fill, copy.Fill);
            Assert.AreEqual(e.Stroke, copy.Stroke);
            Assert.AreEqual(e.StrokeWidth, copy.StrokeWidth);

            copy.Bounds = new RectangleF(100, 100, 50, 60);
            copy.Fill = Color.Green;

            Assert.AreNotEqual(e.Bounds, copy.Bounds);
            Assert.AreNotEqual(e.Fill, copy.Fill);
        }

        /// <summary>
        /// Перемещение линии должно смещать обе точки
        /// на один и тот же вектор.
        /// </summary>
        [TestMethod]
        public void LineShape_MoveBy_MovesBothPoints()
        {
            var line = new LineShape(new PointF(0, 0), new PointF(10, 10))
            {
                Stroke = Color.Black,
                StrokeWidth = 1
            };

            line.MoveBy(new PointF(5, -2));

            Assert.AreEqual(new PointF(5, -2), line.P1);
            Assert.AreEqual(new PointF(15, 8), line.P2);
        }

        /// <summary>
        /// HitTest у прямоугольника должен возвращать true
        /// для точки внутри и false для точки снаружи.
        /// </summary>
        [TestMethod]
        public void RectShape_HitTest_PointInside_ReturnsTrue()
        {
            var r = new RectShape(new RectangleF(10, 10, 20, 20));

            Assert.IsTrue(r.HitTest(new PointF(15, 15)));
            Assert.IsFalse(r.HitTest(new PointF(5, 5)));
        }
    }
}