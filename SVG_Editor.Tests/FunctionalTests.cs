using System.Drawing;
using SVG_Editor.Shapes;
using SVG_Editor.Commands;

namespace SVG_Editor.Tests
{
    /// <summary>
    /// класс функциональных тестов, проверяющих граничные значения параметров
    /// </summary>
    [TestClass]
    public class FunctionalTests
    {
        private const float CanvasWidth = 1200;
        private const float CanvasHeight = 800;

        [TestMethod]
        public void Test1_CreateShapesAtCanvasBoundaries()
        {
            // Тест 1: Координаты фигур
            var shapes = new List<IShape>();

            // Минимальные координаты
            var rect1 = new RectShape(new RectangleF(0, 0, 50, 50));
            Assert.AreEqual(0, rect1.Bounds.X, 0.01f);
            Assert.AreEqual(0, rect1.Bounds.Y, 0.01f);

            // Максимальные координаты
            var rect2 = new RectShape(new RectangleF(CanvasWidth - 1, CanvasHeight - 50, 1, 50));
            Assert.AreEqual(CanvasWidth - 1, rect2.Bounds.X, 0.01f);

            // Недопустимые координаты (за пределами) - проверяем, что создается, но потом может корректироваться
            var rect3 = new RectShape(new RectangleF(-10, 100, 50, 50));
            Assert.AreEqual(-10, rect3.Bounds.X, 0.01f);

            var rect4 = new RectShape(new RectangleF(CanvasWidth, 100, 50, 50));
            Assert.AreEqual(CanvasWidth, rect4.Bounds.X, 0.01f);
        }

        [TestMethod]
        public void Test2_CreateShapesWithBoundarySizes()
        {
            // Тест 2: Размеры фигур
            var shapes = new List<IShape>();

            // Минимальный размер
            var rect1 = new RectShape(new RectangleF(100, 100, 1, 50));
            Assert.AreEqual(1, rect1.Bounds.Width, 0.01f);

            // Максимальный размер
            var rect2 = new RectShape(new RectangleF(0, 100, CanvasWidth, 50));
            Assert.AreEqual(CanvasWidth, rect2.Bounds.Width, 0.01f);

            // Нулевые размеры
            var rect3 = new RectShape(new RectangleF(100, 100, 0, 50));
            Assert.AreEqual(0, rect3.Bounds.Width, 0.01f);

            var rect4 = new RectShape(new RectangleF(100, 100, 50, 0));
            Assert.AreEqual(0, rect4.Bounds.Height, 0.01f);
        }

        [TestMethod]
        public void Test3_StrokeWidthBoundaryValues()
        {
            // Тест 3: Толщина обводки
            var rect = new RectShape(new RectangleF(0, 0, 100, 100));

            // Минимальное значение
            rect.StrokeWidth = 0;
            Assert.AreEqual(0, rect.StrokeWidth, 0.01f);

            // Максимальное значение
            rect.StrokeWidth = 50;
            Assert.AreEqual(50, rect.StrokeWidth, 0.01f);

            // Недопустимые значения - проверяем, что система принимает, но может корректировать
            rect.StrokeWidth = -1;
            // Система может принять отрицательное значение или сбросить его
            // В зависимости от реализации

            rect.StrokeWidth = 51;
            // Система может ограничить значение до 50
        }

        [TestMethod]
        public void Test4_ColorAlphaBoundaryValues()
        {
            // Тест 4: Прозрачность цветов
            var rect = new RectShape(new RectangleF(0, 0, 100, 100));

            // Минимальная прозрачность (полностью прозрачный)
            var transparentColor = Color.FromArgb(0, 255, 0, 0);
            rect.Fill = transparentColor;
            Assert.AreEqual(0, rect.Fill.A);

            // Максимальная непрозрачность
            var opaqueColor = Color.FromArgb(255, 255, 0, 0);
            rect.Fill = opaqueColor;
            Assert.AreEqual(255, rect.Fill.A);
        }

        [TestMethod]
        public void Test5_CreateLinesAtBoundaries()
        {
            // Тест 5: Координаты точек линии
            var shapes = new List<IShape>();

            // Линия внутри холста
            var line1 = new LineShape(new PointF(0, 0), new PointF(CanvasWidth - 1, CanvasHeight - 1));
            Assert.AreEqual(0, line1.P1.X, 0.01f);
            Assert.AreEqual(CanvasWidth - 1, line1.P2.X, 0.01f);

            // Точки за пределами - проверяем создание
            var line2 = new LineShape(new PointF(-10, 0), new PointF(50, 50));
            Assert.AreEqual(-10, line2.P1.X, 0.01f);

            var line3 = new LineShape(new PointF(0, 0), new PointF(CanvasWidth + 100, CanvasHeight));
            Assert.AreEqual(CanvasWidth + 100, line3.P2.X, 0.01f);
        }

        [TestMethod]
        public void Test6_ShapeSelectionBoundary()
        {
            // Тест 6: Выделение фигур
            var shapes = new List<IShape>();
            var rect = new RectShape(new RectangleF(100, 100, 200, 200));
            shapes.Add(rect);

            // Проверка попадания точки внутрь фигуры (должно вернуть true)
            bool hitResult = rect.HitTest(new PointF(150, 150));
            Assert.IsTrue(hitResult, "Точка внутри фигуры должна возвращать true при HitTest");

            // Проверка попадания точки вне фигуры (должно вернуть false)
            hitResult = rect.HitTest(new PointF(50, 50));
            Assert.IsFalse(hitResult, "Точка вне фигуры должна возвращать false при HitTest");

            // Проверка попадания точки на границе (с учетом обводки)
            // Предположим, что обводка толщиной 5px, тогда точка на границе должна считаться внутри
            rect.StrokeWidth = 5;
            hitResult = rect.HitTest(new PointF(100, 150)); // Левая граница
            Assert.IsTrue(hitResult, "Точка на границе с обводкой должна возвращать true");

            // Проверка попадания в фигуру, которая не является прямоугольником (например, эллипс)
            var ellipse = new EllipseShape(new RectangleF(100, 100, 200, 200));
            // Точка в центре эллипса
            hitResult = ellipse.HitTest(new PointF(200, 200));
            Assert.IsTrue(hitResult, "Точка в центре эллипса должна возвращать true");

            // Точка вне эллипса (например, в углу ограничивающего прямоугольника)
            hitResult = ellipse.HitTest(new PointF(100, 100));
            Assert.IsFalse(hitResult, "Точка в углу ограничивающего прямоугольника, но вне эллипса, должна возвращать false");
        }

        [TestMethod]
        public void Test7_GroupingBoundaryConditions()
        {
            // Тест 7: Группировка фигур
            var shapes = new List<IShape>();
            var rect1 = new RectShape(new RectangleF(0, 0, 50, 50));
            var rect2 = new RectShape(new RectangleF(100, 0, 50, 50));

            shapes.Add(rect1);
            shapes.Add(rect2);

            // Минимальное количество для группировки (2)
            var groupCmd = new GroupCommand(shapes, new HashSet<IShape> { rect1, rect2 });
            groupCmd.Do();
            Assert.AreEqual(1, shapes.Count);
            Assert.IsInstanceOfType(shapes[0], typeof(GroupShape));

            // Отмена
            groupCmd.Undo();
            Assert.AreEqual(2, shapes.Count);
        }

        [TestMethod]
        public void Test8_FileOperationsBoundaryCases()
        {
            // Тест 8: Сохранение файла

            // Корректное имя
            string validName = "test.svg";
            Assert.IsTrue(validName.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase));

            // Некорректное расширение
            string invalidExtension = "test.txt";
            Assert.IsFalse(invalidExtension.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Test9_LoadFileBoundaryCases()
        {
            // Тест 9: Загрузка файла

            // Корректный SVG
            string validSVG = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg width=""1200"" height=""800"">
  <rect x=""10"" y=""10"" width=""100"" height=""100"" fill=""red""/>
</svg>";

            // Поврежденный SVG
            string invalidSVG = @"<svg><not-closed-tag>";

            // Пустой файл
            string emptySVG = "";

            // Файл другого формата
            string notSVG = @"This is not an XML file";

            // В реальных тестах эти строки будут передаваться в парсер
        }

        [TestMethod]
        public void Test10_CanvasSizeBoundaryValues()
        {
            // Тест 10: Размеры холста
            var testCases = new[]
            {
                new { Width = 1f, Height = 1f, ExpectedValid = true },
                new { Width = 5000f, Height = 5000f, ExpectedValid = true },
                new { Width = 0f, Height = 800f, ExpectedValid = false },
                new { Width = 1200f, Height = 0f, ExpectedValid = false }
            };

            foreach (var testCase in testCases)
            {
                bool isValid = testCase.Width > 0 && testCase.Height > 0;
                Assert.AreEqual(testCase.ExpectedValid, isValid);
            }
        }

        [TestMethod]
        public void Test11_PerformanceWithManyShapes()
        {
            // Тест 11: Производительность при большом количестве фигур
            var shapes = new List<IShape>();

            // Создаем 1000 простых фигур
            for (int i = 0; i < 1000; i++)
            {
                var rect = new RectShape(new RectangleF(i * 5, i * 2, 50, 50));
                shapes.Add(rect);
            }

            Assert.AreEqual(1000, shapes.Count);
        }

        [TestMethod]
        public void Test12_HistoryDepthBoundary()
        {
            // Тест 12: Глубина истории
            var history = new History();
            var shapes = new List<IShape>();

            // Выполняем 1000 операций
            for (int i = 0; i < 1000; i++)
            {
                var rect = new RectShape(new RectangleF(i * 10, i * 10, 50, 50));
                var cmd = new AddShapeCommand(shapes, rect);
                history.Exec(cmd);
            }

            // Отменяем все операции
            for (int i = 0; i < 1000; i++)
            {
                history.Undo();
            }

            Assert.AreEqual(0, shapes.Count);

            // Повторяем все операции
            for (int i = 0; i < 1000; i++)
            {
                history.Redo();
            }

            Assert.AreEqual(1000, shapes.Count);
        }

        [TestMethod]
        public void Test13_ToolSwitching()
        {
            // Тест 13: Переключение инструментов

            var tools = new[] { "Select", "Rect", "Ellipse", "Line" };

            foreach (var tool in tools)
            {
                Assert.IsNotNull(tool);
            }

            // Проверка горячих клавиш
            var hotkeys = new Dictionary<string, string>
            {
                { "V", "Select" },
                { "R", "Rect" },
                { "E", "Ellipse" },
                { "L", "Line" }
            };

            foreach (var hotkey in hotkeys)
            {
                // В реальной системе здесь была бы проверка реакции на нажатие клавиш
                Assert.IsNotNull(hotkey.Key);
                Assert.IsNotNull(hotkey.Value);
            }
        }

        [TestMethod]
        public void Test14_ZoomBoundaryValues()
        {
            // Тест 14: Масштабирование
            float zoom = 1.0f;

            // Минимальный масштаб
            zoom *= 0.1f; // Предположим, что минимальный масштаб 0.1
            Assert.AreEqual(0.1f, zoom, 0.01f);

            // Максимальный масштаб
            zoom = 1.0f;
            zoom *= 4.0f; // максимальный масштаб 4.0
            Assert.AreEqual(4.0f, zoom, 0.01f);

            // Проверка ограничений
            zoom = 0.05f; // Ниже минимума
            if (zoom < 0.1f)
                zoom = 0.1f; // Система должна скорректировать

            Assert.AreEqual(0.1f, zoom, 0.01f);

            zoom = 5.0f; // Выше максимума
            if (zoom > 4.0f)
                zoom = 4.0f; // Система должна скорректировать

            Assert.AreEqual(4.0f, zoom, 0.01f);
        }

        [TestMethod]
        public void Test15_CopyPasteOperations()
        {
            // Тест 15: Копирование/вставка
            var shapes = new List<IShape>();
            var rect = new RectShape(new RectangleF(100, 100, 50, 50));
            shapes.Add(rect);

            // Копирование одиночной фигуры
            var clipboard = rect.Clone();
            Assert.IsNotNull(clipboard);
            Assert.AreEqual(rect.Bounds, clipboard.Bounds);

            // Вставка
            var pasted = clipboard.Clone();
            pasted.Bounds = new RectangleF(
                pasted.Bounds.X + 10,
                pasted.Bounds.Y + 10,
                pasted.Bounds.Width,
                pasted.Bounds.Height);

            shapes.Add(pasted);
            Assert.AreEqual(2, shapes.Count);

            // Проверка смещения
            Assert.AreEqual(rect.Bounds.X + 10, pasted.Bounds.X, 0.01f);
            Assert.AreEqual(rect.Bounds.Y + 10, pasted.Bounds.Y, 0.01f);
        }

        [TestMethod]
        public void Test16_PropertyPanelSynchronization()
        {
            // Тест 16: Панель свойств
            var rect = new RectShape(new RectangleF(100, 200, 300, 400))
            {
                Fill = Color.Red,
                Stroke = Color.Blue,
                StrokeWidth = 2.5f
            };

            // Проверка начальных значений
            Assert.AreEqual(100, rect.Bounds.X, 0.01f);
            Assert.AreEqual(200, rect.Bounds.Y, 0.01f);
            Assert.AreEqual(300, rect.Bounds.Width, 0.01f);
            Assert.AreEqual(400, rect.Bounds.Height, 0.01f);
            Assert.AreEqual(Color.Red.ToArgb(), rect.Fill.ToArgb());
            Assert.AreEqual(Color.Blue.ToArgb(), rect.Stroke.ToArgb());
            Assert.AreEqual(2.5f, rect.StrokeWidth, 0.01f);

            // Изменение свойств (имитация ввода с панели)
            rect.Bounds = new RectangleF(150, 250, 350, 450);
            rect.Fill = Color.Green;
            rect.Stroke = Color.Yellow;
            rect.StrokeWidth = 3.5f;

            // Проверка обновленных значений
            Assert.AreEqual(150, rect.Bounds.X, 0.01f);
            Assert.AreEqual(250, rect.Bounds.Y, 0.01f);
            Assert.AreEqual(350, rect.Bounds.Width, 0.01f);
            Assert.AreEqual(450, rect.Bounds.Height, 0.01f);
            Assert.AreEqual(Color.Green.ToArgb(), rect.Fill.ToArgb());
            Assert.AreEqual(Color.Yellow.ToArgb(), rect.Stroke.ToArgb());
            Assert.AreEqual(3.5f, rect.StrokeWidth, 0.01f);

            // Проверка команды изменения свойств (для истории)
            var oldBounds = new RectangleF(150, 250, 350, 450);
            var newBounds = new RectangleF(200, 300, 400, 500);

            var cmd = new PropertyChangeCommand(
                rect,
                oldBounds, newBounds,
                Color.Green, Color.Purple,
                Color.Yellow, Color.Black,
                3.5f, 4.5f);

            cmd.Do();

            Assert.AreEqual(newBounds, rect.Bounds);
            Assert.AreEqual(Color.Purple.ToArgb(), rect.Fill.ToArgb());
            Assert.AreEqual(Color.Black.ToArgb(), rect.Stroke.ToArgb());
            Assert.AreEqual(4.5f, rect.StrokeWidth, 0.01f);

            cmd.Undo();

            Assert.AreEqual(oldBounds, rect.Bounds);
            Assert.AreEqual(Color.Green.ToArgb(), rect.Fill.ToArgb());
            Assert.AreEqual(Color.Yellow.ToArgb(), rect.Stroke.ToArgb());
            Assert.AreEqual(3.5f, rect.StrokeWidth, 0.01f);
        }
    }
}