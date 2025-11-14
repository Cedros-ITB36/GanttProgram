using System.Printing;
using System.Windows.Controls;

namespace GanttProgram.Helper
{
    public class PdfExportHelper
    {
        public static void ExportCanvasToPdf(Canvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            PrintDialog printDialog = new()
            {
                PrintQueue = new PrintQueue(new PrintServer(), "Microsoft Print to PDF")
            };
            printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
            printDialog.PrintVisual(canvas, "Gantt Chart");
        }
    }
}
