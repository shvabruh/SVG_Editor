using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVG_Editor;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// Набор модульных тестов для класса Geometry.
    /// Проверяется корректность вычисления расстояний
    /// от точки до отрезка и между точками.
    /// </summary>
    [TestClass]
    public class GeometryTests
    {
        /// <summary>
        /// Точка лежит точно на середине отрезка — ожидаем расстояние 0.
        /// </summary>
        [TestMethod]
        public void DistancePointToSegment_PointOnSegment_ReturnsZero()
        {
            // Arrange
            var a = new PointF(0, 0);
            var b = new PointF(10, 0);
            var p = new PointF(5, 0);

            // Act
            float result = Geometry.DistancePointToSegment(p, a, b);

            // Assert
            Assert.AreEqual(0f, result, 1e-4f);
        }

        /// <summary>
        /// Точка над серединой отрезка — расстояние равно высоте.
        /// </summary>
        [TestMethod]
        public void DistancePointToSegment_PointAboveCenter_ReturnsVerticalDistance()
        {
            var a = new PointF(0, 0);
            var b = new PointF(10, 0);
            var p = new PointF(5, 3);

            float result = Geometry.DistancePointToSegment(p, a, b);

            Assert.AreEqual(3f, result, 1e-4f);
        }

        /// <summary>
        /// Точка ближе к левому концу отрезка — расстояние считается до точки A.
        /// </summary>
        [TestMethod]
        public void DistancePointToSegment_PointBeforeSegment_ReturnsDistanceToA()
        {
            var a = new PointF(0, 0);
            var b = new PointF(10, 0);
            var p = new PointF(-2, 0);

            float result = Geometry.DistancePointToSegment(p, a, b);

            Assert.AreEqual(2f, result, 1e-4f);
        }

        /// <summary>
        /// Проверка вычисления расстояния между двумя точками по формуле.
        /// </summary>
        [TestMethod]
        public void Distance_TwoPoints_ReturnsHypotenuse()
        {
            var a = new PointF(0, 0);
            var b = new PointF(3, 4);

            float result = Geometry.Distance(a, b);

            Assert.AreEqual(5f, result, 1e-4f);
        }
    }
}