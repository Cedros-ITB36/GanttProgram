using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace GanttProgram
{
    public partial class GanttChartWindow : Window
    {
        private const double RowHeight = 30;
        private readonly GanttChartViewModel ViewModel;

        public GanttChartWindow(Projekt project)
        {
            InitializeComponent();
            ViewModel = new GanttChartViewModel(project);
            DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGantt();
        }

        private void GanttScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGantt();
        }

        private void DrawGantt()
        {
            if (ViewModel.Project?.StartDatum == null || ViewModel.PhaseViewModels.Count == 0)
                return;

            GanttCanvas.Children.Clear();

            var startDate = ViewModel.Project.StartDatum.Value;
            var projectEnd = ViewModel.PhaseViewModels.Max(vm => vm.StartDate.AddDays(vm.Phase.Dauer ?? 0));
            var totalDays = Math.Max((projectEnd - startDate).Days, 1);
            var dayWidth = Math.Max(5, (ActualWidth - 100) / totalDays);

            var phaseModels = ViewModel.PhaseViewModels;

            for (var i = 0; i < phaseModels.Count; i++)
            {
                var phaseModel = phaseModels[i];
                var dayOffset = (phaseModel.StartDate - startDate).Days;
                var duration = phaseModel.Phase.Dauer ?? 0;

                var x = dayOffset * dayWidth;
                var y = i * RowHeight;
                var width = duration * dayWidth;
                var height = RowHeight;

                var phaseBar = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = phaseModel.Color,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(phaseBar, x);
                Canvas.SetTop(phaseBar, y);
                GanttCanvas.Children.Add(phaseBar);

                var phaseLabel = new TextBlock
                {
                    Text = $"{phaseModel.Phase.Nummer}: {phaseModel.Phase.Name}",
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(phaseLabel, x);
                Canvas.SetTop(phaseLabel, y + height / 2 - 10);
                GanttCanvas.Children.Add(phaseLabel);
            }

            DrawTimeLine(dayWidth, phaseModels, startDate);

            GanttCanvas.Height = ViewModel.PhaseViewModels.Count * RowHeight + 20;
            GanttCanvas.Width = Math.Max(totalDays * dayWidth, GanttScrollViewer.ViewportWidth - 40);
        }

        private void DrawTimeLine(double dayWidth, ObservableCollection<PhaseViewModel> phaseModels, DateTime startDate)
        {
            var projectEnd = phaseModels.Max(pm => pm.StartDate.AddDays(pm.Phase.Dauer ?? 0));
            var totalDays = (projectEnd - startDate).Days;

            for (var i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);
                var x = i * dayWidth;

                var dayLine = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = phaseModels.Count * RowHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                GanttCanvas.Children.Add(dayLine);

                var dayLabel = new TextBlock
                {
                    Text = date.ToString("dd.MM."),
                    FontSize = 10
                };
                Canvas.SetLeft(dayLabel, x + 2);
                Canvas.SetTop(dayLabel, phaseModels.Count * RowHeight + 5);
                GanttCanvas.Children.Add(dayLabel);
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = "GanttChart.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportCanvasVectorPdf(dialog.FileName);
                MessageBox.Show("PDF export completed.");
            }
        }

        public void ExportCanvasVectorPdf(string filePath)
        {
            var phaseModels = ViewModel.PhaseViewModels;
            if (phaseModels.Count == 0) return;

            var startDate = ViewModel.Project.StartDatum.Value;
            var totalDays = (phaseModels.Max(vm => vm.StartDate.AddDays(vm.Phase.Dauer ?? 0)) - startDate).Days;

            var pdfWidth = 1000d;
            var pdfHeight = Math.Max(phaseModels.Count * RowHeight, 600);

            var dayWidth = Math.Max(5, pdfWidth / Math.Max(totalDays, 1));

            var pdf = new PdfDocument();
            var page = pdf.AddPage();
            page.Width = pdfWidth;
            page.Height = pdfHeight;

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                var font = new XFont("Arial", 10);
                var boldFont = new XFont("Arial Bold", 10, XFontStyleEx.Bold);

                for (var i = 0; i < phaseModels.Count; i++)
                {
                    var phaseModel = phaseModels[i];
                    var dayOffset = (phaseModel.StartDate - startDate).Days;
                    var duration = phaseModel.Phase.Dauer ?? 1;

                    var x = dayOffset * dayWidth;
                    var y = i * RowHeight;
                    var width = duration * dayWidth;
                    var height = RowHeight - 5;

                    var xColor = XColors.Gray;
                    if (phaseModel.Color is SolidColorBrush scb)
                        xColor = XColor.FromArgb(scb.Color.A, scb.Color.R, scb.Color.G, scb.Color.B);

                    gfx.DrawRectangle(new XSolidBrush(xColor), x, y, width, height);

                    gfx.DrawString($"{phaseModel.Phase.Nummer}: {phaseModel.Phase.Name}", boldFont, XBrushes.White,
                        new XRect(x + 3, y + 3, width, height),
                        XStringFormats.TopLeft);
                }

                for (var d = 0; d <= totalDays; d++)
                {
                    var x = d * dayWidth;

                    gfx.DrawLine(XPens.LightGray, x, 0, x, phaseModels.Count * RowHeight);

                    gfx.DrawString(startDate.AddDays(d).ToString("dd.MM."), font, XBrushes.Black,
                        new XRect(x + 2, phaseModels.Count * RowHeight + 2, dayWidth, 12),
                        XStringFormats.TopLeft);
                }
            }

            pdf.Save(filePath);
        }
    }
}