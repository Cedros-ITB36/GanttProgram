using GanttProgram.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GanttProgram
{
    /// <summary>
    /// Interaktionslogik für GanttChartWindow.xaml
    /// </summary>
    public partial class GanttChartWindow : Window
    {
        private const double DayWidth = 40;
        private const double RowHeight = 130;

        public GanttChartWindow()
        {
            var projekt = new Projekt
            {
                Id = 1,
                Bezeichnung = "Test",
                StartDatum = new DateTime(2025, 11, 12)
            };
            var phasen = new List<Phase>
            {
                new Phase{ Id = 1, Nummer = "A", Name = "Planning", Dauer = 5, Vorgaenger = null, ProjektId = 1 },
                new Phase{ Id = 2, Nummer = "B", Name = "Implementation", Dauer = 10, Vorgaenger = 1, ProjektId = 1 },
                new Phase{ Id = 3, Nummer = "C", Name = "Testing", Dauer = 7, Vorgaenger = 2, ProjektId = 1 },
                new Phase{ Id = 4, Nummer = "D", Name = "Deployment", Dauer = 3, Vorgaenger = 2, ProjektId = 1 },
            };
            InitializeComponent();

            DrawGantt(projekt, phasen);
        }

        private void DrawGantt(Projekt project, List<Phase> phases)
        {
            if (project == null) return;
            if (phases == null) return;
            if (project.StartDatum == null) return;

            var start = project.StartDatum.Value;
            var phaseStarts = new Dictionary<int, DateTime>();
            foreach (var phase in phases)
            {
                if (phase.Vorgaenger == null)
                {
                    phaseStarts[phase.Id] = start;
                    continue;
                }
                var predecessor = phases.First(p => p.Id == phase.Vorgaenger);
                var predecessorStart = phaseStarts[predecessor.Id];
                var predecessorEnd = predecessorStart.AddDays(predecessor.Dauer ?? 0);
                phaseStarts[phase.Id] = predecessorEnd;
            }
            for (int i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                var phaseStart = phaseStarts[phase.Id];
                var offsetDays = (phaseStart - start).Days;
                var duration = phase.Dauer ?? 0;

                var x = offsetDays * DayWidth;
                var y = i * RowHeight;
                var width = duration * DayWidth;
                var height = RowHeight;

                var bar = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromRgb(200, 0, 0)),
                    RadiusX = 3,
                    RadiusY = 3
                };

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);

                var label = new TextBlock
                {
                    Text = $"{phase.Nummer}: {phase.Name}",
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(label, x);
                Canvas.SetTop(label, y);

                GanttCanvas.Children.Add(bar);
                GanttCanvas.Children.Add(label);
            }

            DrawTimeLine(start, phases, phaseStarts);
        }

        private void DrawTimeLine(DateTime start, List<Phase> phases, Dictionary<int, DateTime> phaseStarts)
        {
            var maxEnd = phaseStarts.Max(k =>
            {
                var phase = phases.First(p => p.Id == k.Key);
                return k.Value.AddDays(phase.Dauer ?? 0);
            });

            var totalDays = (maxEnd - start).Days;

            for (int i = 0; i < totalDays; i++)
            {
                var date = start.AddDays(i);
                var x = i * DayWidth;
                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = phases.Count * RowHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };

                GanttCanvas.Children.Add(line);

                var label = new TextBlock
                {
                    Text = date.ToString("dd.MM."),
                    FontSize = 10
                };

                Canvas.SetLeft(label, x + 2);
                Canvas.SetTop(label, phases.Count * RowHeight + 5);

                GanttCanvas.Children.Add(label);
            }
        }
    }
}
