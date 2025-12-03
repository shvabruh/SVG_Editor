namespace SVG_Editor
{
    /// <summary>
    /// Вспомогательный статический класс с базовыми геометрическими вычислениями,
    /// используемыми в редакторе. 
    /// Содержит функции для определения расстояния от точки до отрезка 
    /// и вычисления евклидова расстояния между двумя точками.
    /// Эти методы применяются при hit-test линий (определение попадания курсора)
    /// и при точных вычислениях положения фигур.
    public static class Geometry
    {
        /// <summary>
        /// Вычисляет минимальное расстояние от точки p до отрезка [a, b].
        /// Используется для hit-test линий: определяет,
        /// насколько близко курсор находится к нарисованному отрезку.
        /// Алгоритм: проецирует точку p на линию ab, проверяет,
        /// лежит ли проекция внутри отрезка, и возвращает расстояние либо
        /// до конца отрезка, либо до проекции.
        /// </summary>
        /// <param name="p">Точка, до которой измеряется расстояние.</param>
        /// <param name="a">Начало отрезка.</param>
        /// <param name="b">Конец отрезка.</param>
        /// <returns>Минимальное расстояние от p до сегмента ab.</returns>
        public static float DistancePointToSegment(PointF p, PointF a, PointF b)
        {
            var vx = b.X - a.X; var vy = b.Y - a.Y;
            var wx = p.X - a.X; var wy = p.Y - a.Y;
            var c1 = vx * wx + vy * wy;
            if (c1 <= 0) return Distance(p, a);
            var c2 = vx * vx + vy * vy;
            if (c2 <= c1) return Distance(p, b);
            var t = c1 / c2;
            var proj = new PointF(a.X + t * vx, a.Y + t * vy);
            return Distance(p, proj);
        }

        /// <summary>
        /// Вычисляет евклидово расстояние между точками a и b.
        /// Используется геометрическими методами редактора для точных расчётов
        /// (в частности — при определении расстояния от точки до отрезка).
        /// Формула: sqrt((ax - bx)^2 + (ay - by)^2).
        /// </summary>
        /// <param name="a">Первая точка.</param>
        /// <param name="b">Вторая точка.</param>
        /// <returns>Расстояние между a и b.</returns>
        public static float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
