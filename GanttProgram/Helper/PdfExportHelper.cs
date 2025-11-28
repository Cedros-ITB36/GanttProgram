using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GanttProgram.Helper
{
    public static class PdfExportHelper
    {
        public static void ExportCanvasToPdf(Canvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);

            const double paddingMm = 15.0;
            const double dpi = 96.0;
            const double padding = (paddingMm / 25.4) * dpi;

            double contentWidth = 0;
            double contentHeight = 0;
            foreach (UIElement child in canvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    var right = Canvas.GetLeft(fe) + fe.ActualWidth;
                    var bottom = Canvas.GetTop(fe) + fe.ActualHeight;
                    if (right > contentWidth)
                        contentWidth = right;
                    if (bottom > contentHeight)
                        contentHeight = bottom;
                }
            }
            if (contentWidth == 0)
                contentWidth = canvas.ActualWidth;
            if (contentHeight == 0)
                contentHeight = canvas.ActualHeight;

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                context.PushTransform(new TranslateTransform(padding - 35, padding));
                context.DrawRectangle(new VisualBrush(canvas), null, new Rect(0, 0, contentWidth, contentHeight));
            }

            var printQueue = new PrintQueue(new PrintServer(), "Microsoft Print to PDF");
            printQueue.DefaultPrintTicket.PageOrientation = PageOrientation.Landscape;

            PrintDialog printDialog = new()
            {
                PrintQueue = printQueue,
                PrintTicket = { PageOrientation = PageOrientation.Landscape }
            };

            var result = printDialog.ShowDialog();
            if (result is null or false) return;

            drawingVisual.Transform = new ScaleTransform(1, 1);
            drawingVisual.Offset = new Vector(0, 0);

            printDialog.PrintVisual(drawingVisual, "Gantt Chart");
            MessageBox.Show("PDF-Export erfolgreich abgeschlossen.", "Export abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
