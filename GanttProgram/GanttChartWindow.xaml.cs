using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GanttProgram.Helper;
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

            var criticalPathPhases = CriticalPathHelper.GetCriticalPathPhasen(project.Id);
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

                phaseModel.X = dayOffset * dayWidth;
                phaseModel.Y = i * RowHeight;
                phaseModel.Width = phaseModel.ActualDuration * dayWidth - 1;
                phaseModel.BufferedWidth = phaseModel.BufferedDuration * dayWidth - 1;
                phaseModel.Height = RowHeight;
            }

            DrawBuffers(phaseModels);
            DrawPhases(phaseModels);
            DrawTimeLine(dayWidth, phaseModels, startDate);
            DrawWeekends(dayWidth, phaseModels, startDate, totalDays);
            DrawDayLines(dayWidth, phaseModels, startDate);
            DrawPhaseLabels(phaseModels);

            GanttCanvas.Height = ViewModel.PhaseViewModels.Count * RowHeight + 20;
            GanttCanvas.Width = Math.Max(totalDays * dayWidth, GanttScrollViewer.ViewportWidth - 40);
        }

        private void DrawBuffers(ObservableCollection<PhaseViewModel> phaseModels)
        {
            for (var i = 0; i < phaseModels.Count; i++)
            {
                var phaseModel = phaseModels[i];

                if (phaseModel.IsCriticalPath)
                    continue;

                var fillBrush = phaseModel.Color.Clone();
                fillBrush.Opacity = 0.4;

                var bufferBar = new Rectangle
                {
                    Width = phaseModel.BufferedWidth,
                    Height = phaseModel.Height,
                    Fill = fillBrush,
                    RadiusX = 3,
                    RadiusY = 3,
                    Stroke = phaseModel.Color,
                    Margin = new Thickness(1, 0, 0, 0)
                };
                Canvas.SetLeft(bufferBar, phaseModel.X);
                Canvas.SetTop(bufferBar, phaseModel.Y);

                GanttCanvas.Children.Add(bufferBar);
            }
        }

        private void DrawPhases(ObservableCollection<PhaseViewModel> phaseModels)
        {
            for (var i = 0; i < phaseModels.Count; i++)
            {
                var phaseModel = phaseModels[i];

                var phaseBar = new Rectangle
                {
                    Width = phaseModel.Width,
                    Height = phaseModel.Height,
                    Fill = phaseModel.Color,
                    RadiusX = 3,
                    RadiusY = 3,
                    Stroke = phaseModel.IsCriticalPath ? Brushes.Black : Brushes.Transparent,
                    StrokeThickness = 1.3,
                    Margin = new Thickness(1, 0, 0, 0)
                };
                Canvas.SetLeft(phaseBar, phaseModel.X);
                Canvas.SetTop(phaseBar, phaseModel.Y);

                GanttCanvas.Children.Add(phaseBar);
            }
        }

        private void DrawPhaseLabels(ObservableCollection<PhaseViewModel> phaseModels)
        {
            for (var i = 0; i < phaseModels.Count; i++)
            {
                var phaseModel = phaseModels[i];

                var phaseLabel = new TextBlock
                {
                    Text = $"{phaseModel.Phase.Nummer}: {phaseModel.Phase.Name}",
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                };
                Canvas.SetLeft(phaseLabel, phaseModel.X);
                Canvas.SetTop(phaseLabel, phaseModel.Y + phaseModel.Height / 2 - 10);

                GanttCanvas.Children.Add(phaseLabel);
            }
        }

        private void DrawTimeLine(double dayWidth, ObservableCollection<PhaseViewModel> phaseModels, DateTime startDate)
        {
            var projectEnd = phaseModels.Max(pm => pm.StartDate.AddDays(pm.Phase.Dauer ?? 0));
            var totalDays = (projectEnd - startDate).Days;

            for (var i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);
                var x = i * dayWidth;

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

        private void DrawDayLines(double dayWidth, ObservableCollection<PhaseViewModel> phaseModels, DateTime startDate)
        {
            var projectEnd = phaseModels.Max(pm => pm.StartDate.AddDays(pm.Phase.Dauer ?? 0));
            var totalDays = (projectEnd - startDate).Days;

            for (var i = 0; i < totalDays; i++)
            {
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
            }
        }

        private void DrawWeekends(double dayWidth, ObservableCollection<PhaseViewModel> phaseModels, DateTime startDate, int totalDays)
        {
            for (var i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);

                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    var x = i * dayWidth;

                    var weekendRect = new Rectangle
                    {
                        Width = dayWidth,
                        Height = phaseModels.Count * RowHeight,
                        Fill = Brushes.Gray,
                        Opacity = 0.8
                    };

                    Canvas.SetLeft(weekendRect, x);
                    Canvas.SetTop(weekendRect, 0);

                    GanttCanvas.Children.Add(weekendRect);
                }
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
                var font = new XFont("Arial", 12);
                var boldFont = new XFont("Arial Bold", 12, XFontStyleEx.Bold);

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