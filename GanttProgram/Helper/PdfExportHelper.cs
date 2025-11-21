using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace GanttProgram.Helper
{
    public static class PdfExportHelper
    {
        public static void ExportCanvasToPdf(Canvas canvas)
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
            var result = printDialog.ShowDialog();
            if (result is null or false) return;
            printDialog.PrintVisual(canvas, "Gantt Chart");
            MessageBox.Show("PDF-Export erfolgreich abgeschlossen.", "Export abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
