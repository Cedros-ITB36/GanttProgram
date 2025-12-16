using System.Printing;
using System.Windows.Controls;
using System.Windows.Media;

namespace GanttProgram.Helper
{
    public static class PdfExportHelper
    {
        public static void ExportCanvasToPdf(Canvas canvas, string projectTitle)
        {
            ArgumentNullException.ThrowIfNull(canvas);

            PrintDialog printDialog = new()
            {
                PrintQueue = new PrintQueue(new PrintServer(), "Microsoft Print to PDF"),
                PrintTicket =
                {
                    PageOrientation = PageOrientation.Landscape
                }
            };
            if (VisualTreeHelper.GetParent(canvas) is Border parentBorder)
                printDialog.PrintVisual(parentBorder, projectTitle);
            else
                printDialog.PrintVisual(canvas, projectTitle);
        }
    }
}
