using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GanttProgram
{
    public partial class GanttChartWindow
    {
        private const double RowHeight = 30;
        private readonly GanttChartViewModel _viewModel;

        public GanttChartWindow(Project project)
        {
            InitializeComponent();
            _viewModel = new GanttChartViewModel(project);
            DataContext = _viewModel;
            Closing += Window_Closing;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGantt();
        }

        private static void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            if (mainWindow != null) return;
            mainWindow = new MainWindow();
            mainWindow.Show();
            mainWindow.ActivateProjectTab();
        }

        private void GanttScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGantt();
        }

        //TODO Änderung der letzten Phase führt zu falscher Anzeige
        private void DrawGantt()
        {
            if (_viewModel.Project?.StartDate == null)
            {
                MessageBox.Show("Zum Zeichnen des Gantt-Diagramms benötigt das Projekt einen Startzeitpunkt.", "Fehlende Daten",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            if (_viewModel.PhaseViewModels.Count == 0)
            {
                MessageBox.Show("Zum Zeichnen des Gantt-Diagramms benötigt das Projekt mindestens eine Phase.", "Fehlende Daten",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            GanttCanvas.Children.Clear();

            var startDate = _viewModel.Project.StartDate.Value;
            var projectEnd = _viewModel.PhaseViewModels.Max(vm => vm.StartDate.AddDays(vm.Phase.Duration ?? 0));
            var totalDays = Math.Max((projectEnd - startDate).Days, 1);
            var dayWidth = Math.Max(5, (ActualWidth - 100) / totalDays);

            var phaseModels = _viewModel.PhaseViewModels;

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

            GanttCanvas.Height = _viewModel.PhaseViewModels.Count * RowHeight + 20;
            double rightPadding = 20; 
            double maxRight = 0;

            foreach (var phaseModel in phaseModels)
            {
                var labelText = $"{phaseModel.Phase.Number}: {phaseModel.Phase.Name}";
                var labelWidth = GetLabelWidth(labelText);

                double labelRight = phaseModel.X + 8 + labelWidth + 15;
                if (labelRight > maxRight)
                    maxRight = labelRight;
            }

            GanttCanvas.Width = Math.Max(maxRight + rightPadding, Math.Max(totalDays * dayWidth, GanttScrollViewer.ViewportWidth - 40));
        }

        private double GetLabelWidth(string labelText)
        {
            return new FormattedText(
                labelText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                14,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            ).Width;
        }

        private void DrawBuffers(ObservableCollection<GanttPhaseViewModel> phaseModels)
        {
            foreach (var phaseModel in phaseModels)
            {
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
                    Margin = new Thickness(1, 0, 0, 0),
                    ToolTip = phaseModel.ToolTip
                };
                Canvas.SetLeft(bufferBar, phaseModel.X);
                Canvas.SetTop(bufferBar, phaseModel.Y);

                GanttCanvas.Children.Add(bufferBar);
            }
        }

        private void DrawPhases(ObservableCollection<GanttPhaseViewModel> phaseModels)
        {
            foreach (var phaseModel in phaseModels)
            {
                var phaseBar = new Rectangle
                {
                    Width = phaseModel.Width,
                    Height = phaseModel.Height,
                    Fill = phaseModel.Color,
                    RadiusX = 3,
                    RadiusY = 3,
                    Stroke = phaseModel.IsCriticalPath ? Brushes.Red : Brushes.Transparent,
                    StrokeThickness = 1.5,
                    Margin = new Thickness(1, 0, 0, 0),
                    ToolTip = phaseModel.ToolTip
                };
                Canvas.SetLeft(phaseBar, phaseModel.X);
                Canvas.SetTop(phaseBar, phaseModel.Y);

                GanttCanvas.Children.Add(phaseBar);
            }
        }

        private void DrawPhaseLabels(ObservableCollection<GanttPhaseViewModel> phaseModels)
        {
            foreach (var phaseModel in phaseModels)
            {
                var phaseLabel = new TextBlock
                {
                    Text = $"{phaseModel.Phase.Number}: {phaseModel.Phase.Name}",
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    ToolTip = phaseModel.ToolTip
                };
                Canvas.SetLeft(phaseLabel, phaseModel.X);
                Canvas.SetTop(phaseLabel, phaseModel.Y + phaseModel.Height / 2 - 10);

                GanttCanvas.Children.Add(phaseLabel);
            }
        }

        private void DrawTimeLine(double dayWidth, ObservableCollection<GanttPhaseViewModel> phaseModels, DateTime startDate)
        {
            var projectEnd = phaseModels.Max(pm => pm.StartDate.AddDays(pm.Phase.Duration ?? 0));
            var totalDays = (projectEnd - startDate).Days;

            for (var i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);
                var x = i * dayWidth;

                var dayLabel = new TextBlock
                {
                    Text = date.ToString("dd.MM."),
                    FontSize = 10,
                    ToolTip = date.ToString("dddd, dd.MM.yyyy")
                };
                Canvas.SetLeft(dayLabel, x + 2);
                Canvas.SetTop(dayLabel, phaseModels.Count * RowHeight + 5);

                GanttCanvas.Children.Add(dayLabel);
            }
        }

        private void DrawDayLines(double dayWidth, ObservableCollection<GanttPhaseViewModel> phaseModels, DateTime startDate)
        {
            var projectEnd = phaseModels.Max(pm => pm.StartDate.AddDays(pm.Phase.Duration ?? 0));
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

        private void DrawWeekends(double dayWidth, ObservableCollection<GanttPhaseViewModel> phaseModels, DateTime startDate, int totalDays)
        {
            for (var i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);

                if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) continue;
                var x = i * dayWidth;

                var weekendRect = new Rectangle
                {
                    Width = dayWidth,
                    Height = phaseModels.Count * RowHeight,
                    Fill = Brushes.Gray,
                    Opacity = 0.8,
                    ToolTip = "Wochenende"
                };

                Canvas.SetLeft(weekendRect, x);
                Canvas.SetTop(weekendRect, 0);

                GanttCanvas.Children.Add(weekendRect);
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PdfExportHelper.ExportCanvasToPdf(GanttCanvas, _viewModel.Project.Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF-Export fehlgeschlagen:\n{ex.Message}", "Export fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}