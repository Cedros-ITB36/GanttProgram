using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace GanttProgram.Helper
{
    public static class PdfExportHelper
    {
        public static void ExportCanvasToPdf(Canvas canvas, string projectTitle)
        {
            ArgumentNullException.ThrowIfNull(canvas);

            const double paddingMm = 15.0;
            const double dpi = 96.0;
            double paddingPx = (paddingMm / 25.4) * dpi;

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

            int bmpWidth = (int)(contentWidth + 2 * paddingPx);
            int bmpHeight = (int)(contentHeight + 2 * paddingPx);

            string safeTitle = string.IsNullOrWhiteSpace(projectTitle)
                ? "Unbenannt"
                : string.Concat(projectTitle.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"GanttChart_{safeTitle}.pdf";

            var sfd = new SaveFileDialog
            {
                Title = "PDF speichern",
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                FileName = fileName,
                InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                )
            };

            if (sfd.ShowDialog() != true)
                return;

            string pdfPath = sfd.FileName;

            var rtb = new RenderTargetBitmap(bmpWidth, bmpHeight, dpi, dpi, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.PushTransform(new TranslateTransform(paddingPx, paddingPx));
                ctx.DrawRectangle(new VisualBrush(canvas), null, new Rect(0, 0, contentWidth, contentHeight));
            }
            rtb.Render(dv);

            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(ms);

                using (var pdf = new PdfDocument())
                {
                    var page = pdf.AddPage();
                    page.Width = bmpWidth * 72.0 / dpi;   
                    page.Height = bmpHeight * 72.0 / dpi;

                    using (var gfx = XGraphics.FromPdfPage(page))
                    using (var img = XImage.FromStream(new MemoryStream(ms.ToArray())))
                    {
                        gfx.DrawImage(img, 0, 0, page.Width, page.Height);
                    }
                    pdf.Save(pdfPath);
                }
            }

            MessageBox.Show("PDF-Export erfolgreich abgeschlossen.", "Export abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
