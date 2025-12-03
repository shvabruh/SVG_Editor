using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVG_Editor.Shapes
{
    /// <summary>
    /// Базовый интерфейс любой фигуры, которая может быть нарисована на холсте.
    /// Определяет общие свойства (границы, заливка, обводка) и операции.
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// Ограничивающий прямоугольник фигуры в координатах холста.
        /// Используется для отрисовки, выделения и hit-test.
        /// </summary>
        RectangleF Bounds { get; set; }

        /// <summary>
        /// Отрисовывает фигуру на переданном объекте Graphics.
        /// </summary>
        void Draw(Graphics g);

        /// <summary>
        /// Проверяет, попадает ли указанная точка внутрь фигуры
        /// (с учётом её формы и текущих параметров).
        /// </summary>
        bool HitTest(PointF p);

        /// <summary>
        /// Сдвигает фигуру на заданный вектор по X и Y.
        /// </summary>
        void MoveBy(PointF d);

        /// <summary>
        /// Цвет заливки фигуры. Полностью прозрачное значение означает отсутствие заливки.
        /// </summary>
        Color Fill { get; set; }

        /// <summary>
        /// Цвет обводки (контура) фигуры.
        /// </summary>
        Color Stroke { get; set; }

        /// <summary>
        /// Толщина линии обводки в пикселях.
        /// </summary>
        float StrokeWidth { get; set; }

        /// <summary>
        /// Создаёт глубокую копию фигуры со всеми её параметрами.
        /// </summary>
        IShape Clone();
    }
}
