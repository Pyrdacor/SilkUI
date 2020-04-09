using System.Drawing;
using Silk.NET.Windowing.Common;

namespace SilkUI
{
    public enum LineStyle
    {
        Solid,
        Dotted,
        Dashed
    }

    public interface IControlRenderer
    {
        void StartRenderCycle();
        void EndRenderCycle();
        void RemoveRenderObject(int renderObjectIndex);
        void ReplaceRenderObjectWithFollowingDrawCall(int renderObjectIndex);
        int DrawRectangle(int x, int y, int width, int height, Color color, int lineSize);
        int FillRectangle(int x, int y, int width, int height, Color color);
        int DrawRectangleLine(int x, int y, int width, int height, Color color, LineStyle lineStyle);
        int DrawImage(int x, int y, Bitmap image, Color? colorOverlay = null);
        int FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color);
        int FillPolygon(Color color, params Point[] points);
        int DrawShadow(int x, int y, int width, int height, Color color, int blurRadius, bool inset);
        int DrawText(int x, int y, string text, Font font, Color color);
        int DrawText(Rectangle bounds, string text, Font font, Color color, HorizontalAlignment horizontalAlignment,
            VertictalAlignment vertictalAlignment, bool wordWrap, TextOverflow textOverflow);
    }

    public interface IControlRendererFactory
    {
        IControlRenderer CreateControlRenderer(IView view);
    }
}