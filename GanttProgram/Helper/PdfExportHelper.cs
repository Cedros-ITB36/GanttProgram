using System.Printing;
using System.Windows.Controls;

namespace GanttProgram.Helper
{
    public class PdfExportHelper
    {
        //TODO: catch for abbrechen button
        public static void ExportCanvasToPdf(Canvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            PrintDialog printDialog = new()
            {
                PrintQueue = new PrintQueue(new PrintServer(), "Microsoft Print to PDF")
            };
            printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
            printDialog.PrintVisual(canvas, "Gantt Chart");

            //bool? result = printDialog.ShowDialog();
            //if (result == true)
            //{
            //    printDialog.PrintVisual(canvas, "Gantt Chart");
            //}
            //else
            //{
            //    // Hier wurde auf "Abbrechen" geklickt
            //    // Eigene Logik einfügen
            //}
        }
    }
}
