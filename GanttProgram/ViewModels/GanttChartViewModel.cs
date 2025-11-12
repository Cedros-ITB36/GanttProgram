using GanttProgram.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace GanttProgram.ViewModels
{
    public class PhaseViewModel
    {
        public required Phase Phase { get; set; }
        public DateTime StartDate { get; set; }
        public required Brush Color { get; set; }
    }

    public class GanttChartViewModel
    {
        private static readonly Brush[] PhaseColors =
        [
            Brushes.CornflowerBlue,
            Brushes.MediumSeaGreen,
            Brushes.Goldenrod,
            Brushes.IndianRed,
            Brushes.MediumOrchid,
            Brushes.DarkKhaki,
            Brushes.CadetBlue,
            Brushes.Peru,
            Brushes.Red,
            Brushes.Green,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Brown
        ];

        public Projekt Project { get; }
        public ObservableCollection<PhaseViewModel> PhaseViewModels { get; } = [];

        public GanttChartViewModel(Projekt project)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));

            if (project.Phasen == null || project.Phasen.Count == 0 || project.StartDatum == null)
                return;

            ComputePhaseStarts();
        }

        private void ComputePhaseStarts()
        {
            var start = Project.StartDatum.Value;
            var phaseStarts = new Dictionary<Phase, DateTime>();

            var phases = Project.Phasen.ToList();

            bool allCalculated = false;
            while (!allCalculated)
            {
                allCalculated = true;
                for (int i = 0; i < phases.Count; i++)
                {
                    var phase = phases[i];

                    if (phaseStarts.ContainsKey(phase)) continue;

                    if (phase.Vorgaenger == null || phase.Vorgaenger.Count == 0)
                    {
                        phaseStarts[phase] = start;
                    }
                    else
                    {
                        if (!phase.Vorgaenger.All(v => phaseStarts.ContainsKey(v.VorgaengerPhase)))
                        {
                            allCalculated = false;
                            continue;
                        }
                        var maxEnd = phase.Vorgaenger
                            .Select(v => phaseStarts[v.VorgaengerPhase].AddDays(
                                Project.Phasen.First(p => p.Id == v.VorgaengerId).Dauer ?? 0))
                            .Max();
                        phaseStarts[phase] = maxEnd;
                    }

                    PhaseViewModels.Add(new PhaseViewModel
                    {
                        Phase = phase,
                        StartDate = phaseStarts[phase],
                        Color = PhaseColors[i % PhaseColors.Length]
                    });
                }
            }
        }
    }
}