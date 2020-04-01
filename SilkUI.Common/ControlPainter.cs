using System;
using System.Drawing;

namespace SilkUI
{
    public static class ControlPainter
    {
        private static void DrawSetBorder(Control control, ref int? renderRef, ControlRenderer renderer, StlyeDirection direction,
            bool inset, ColorValue color, int lineSize, Rectangle rectangle)
        {
            var points = direction switch
            {
                StlyeDirection.Top => new Point[4]
                {
                    new Point(rectangle.Left, rectangle.Top),
                    new Point(rectangle.Right, rectangle.Top),
                    new Point(rectangle.Right - lineSize, rectangle.Top + lineSize),
                    new Point(rectangle.Left + lineSize, rectangle.Top + lineSize),                    
                },
                StlyeDirection.Right => new Point[4]
                {
                    new Point(rectangle.Right - lineSize, rectangle.Top + lineSize),
                    new Point(rectangle.Right, rectangle.Top),
                    new Point(rectangle.Right, rectangle.Bottom),
                    new Point(rectangle.Right - lineSize, rectangle.Bottom - lineSize),
                },
                StlyeDirection.Bottom => new Point[4]
                {
                    new Point(rectangle.Left + lineSize, rectangle.Bottom - lineSize),
                    new Point(rectangle.Right - lineSize, rectangle.Bottom - lineSize),
                    new Point(rectangle.Right, rectangle.Bottom),
                    new Point(rectangle.Left, rectangle.Bottom),                    
                },
                StlyeDirection.Left => new Point[4]
                {
                    new Point(rectangle.Left, rectangle.Top),
                    new Point(rectangle.Left + lineSize, rectangle.Top + lineSize),
                    new Point(rectangle.Left + lineSize, rectangle.Bottom - lineSize),
                    new Point(rectangle.Left, rectangle.Bottom),
                },
                _ => throw new ArgumentException("Invalid style direction.")
            };

            ColorValue drawColor;

            if (inset)
                drawColor = (int)direction % 3 == 0 ? color.Darken(0.5f) : color.Lighten(0.25f);
            else
                drawColor = (int)direction % 3 == 0 ? color.Lighten(0.25f) : color.Darken(0.5f);

            renderRef = renderer.FillPolygon(control, renderRef, drawColor, points);
        }

        public static void DrawBorder(Control control, ref int? renderRef, ControlRenderer renderer, StlyeDirection direction,
            BorderLineStyle lineStyle, ColorValue color, int lineSize, Rectangle rectangle)
        {
            if (lineStyle == BorderLineStyle.Inset || lineStyle == BorderLineStyle.Outset)
            {
                DrawSetBorder(control, ref renderRef, renderer, direction, lineStyle == BorderLineStyle.Inset,
                    color, lineSize, rectangle);
                return;
            }

            int x = direction switch
            {
                StlyeDirection.Right => rectangle.X + rectangle.Width - lineSize,
                _ => rectangle.X
            };
            int y = direction switch
            {
                StlyeDirection.Top => rectangle.Y,
                StlyeDirection.Bottom => rectangle.Y + rectangle.Height - lineSize,
                _ => rectangle.Y + lineSize
            };
            int width = (int)direction % 2 == 0 ? rectangle.Width : lineSize;
            int height = (int)direction % 2 == 0 ? lineSize : rectangle.Height - 2 * lineSize;

            switch (lineStyle)
            {
                case BorderLineStyle.Solid:
                    renderRef = renderer.DrawRectangleLine(control, renderRef, x, y, width, height, color, LineStyle.Solid);
                    break;
                case BorderLineStyle.Dotted:
                    renderRef = renderer.DrawRectangleLine(control, renderRef, x, y, width, height, color, LineStyle.Dotted);
                    break;
                case BorderLineStyle.Dashed:
                    renderRef = renderer.DrawRectangleLine(control, renderRef, x, y, width, height, color, LineStyle.Dashed);
                    break;
                case BorderLineStyle.Double:
                case BorderLineStyle.Groove:
                case BorderLineStyle.Ridge:
                    // TODO
                    break;
                case BorderLineStyle.None:
                default:
                    break;
            }
        }

        public static void DrawShadow(Control control, ref int? renderRef, ControlRenderer renderer, Rectangle boxRectangle,
            int offsetX, int offsetY, Color color, int blurRadius = 0, int spreadRadius = 0, bool inset = false)
        {
            int x = boxRectangle.X + offsetX - spreadRadius;
            int y = boxRectangle.Y + offsetY - spreadRadius;
            int width = boxRectangle.Width + spreadRadius * 2;
            int height = boxRectangle.Height + spreadRadius * 2;
            
            renderRef = renderer.DrawShadow(control, renderRef, x, y, width, height, color, blurRadius, inset);
        }
    }
}