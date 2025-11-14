using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace GanttProgram.ViewModels
{
    public class PhaseViewModel
    {
        public required Phase Phase { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public required Brush Color { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double BufferedWidth { get; set; }
        public double Height { get; set; }
        public bool IsCriticalPath { get; set; }
        public int ActualDuration { get; set; }
        public int BufferedDuration { get; set; }
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
            var projectStart = Project.StartDatum.Value;
            var phaseStarts = new Dictionary<Phase, DateTime>();
            var phaseEnds = new Dictionary<Phase, DateTime>();

            var phases = Project.Phasen.ToList();
            var criticalPhases = CriticalPathHelper.GetCriticalPathPhasen(Project.Id);

            var allCalculated = false;
            while (!allCalculated)
            {
                allCalculated = true;
                for (var i = 0; i < phases.Count; i++)
                {
                    var phase = phases[i];

                    if (phaseStarts.ContainsKey(phase)) continue;

                    if (phase.Vorgaenger == null || phase.Vorgaenger.Count == 0)
                    {
                        phaseStarts[phase] = projectStart;
                    }
                    else
                    {
                        if (!phase.Vorgaenger.All(v => phaseEnds.ContainsKey(v.VorgaengerPhase)))
                        {
                            allCalculated = false;
                            continue;
                        }
                        var maxEnd = phase.Vorgaenger
                            .Select(v => phaseEnds[v.VorgaengerPhase])
                            .Max();
                        phaseStarts[phase] = maxEnd;
                    }

                    var baseDuration = phase.Dauer ?? 0;

                    var extraWeekendDays = 0;
                    var lastExtra = -1;

                    while (lastExtra != extraWeekendDays)
                    {
                        lastExtra = extraWeekendDays;
                        var totalDays = baseDuration + extraWeekendDays;
                        extraWeekendDays = CountWeekendDaysInRange(phaseStarts[phase], totalDays);
                    }

                    var actualDuration = baseDuration + extraWeekendDays;
                    var endDate = phaseStarts[phase].AddDays(actualDuration);

                    phaseEnds[phase] = endDate;
                }
            }

            var phaseLatestEnd = new Dictionary<Phase, DateTime>();
            var projectEnd = phaseEnds.Values.Max();

            foreach (var phase in phases)
                phaseLatestEnd[phase] = projectEnd;

            foreach (var phase in phases)
            {
                var successors = Project.Phasen
                    .Where(p => p.Vorgaenger != null && p.Vorgaenger.Any(v => v.VorgaengerPhase.Id == phase.Id))
                    .ToList();

                if (successors.Count == 0)
                    continue;

                var minSuccessorStart = successors.Min(s => phaseStarts[s]);
                phaseLatestEnd[phase] = minSuccessorStart;
            }

            for (var i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                var actualDuration = (phaseEnds[phase] - phaseStarts[phase]).Days;
                var buffer = (phaseLatestEnd[phase] - phaseEnds[phase]).Days;

                PhaseViewModels.Add(new PhaseViewModel
                {
                    Phase = phase,
                    StartDate = phaseStarts[phase],
                    EndDate = phaseEnds[phase],
                    Color = PhaseColors[i % PhaseColors.Length],
                    ActualDuration = actualDuration,
                    Width = actualDuration,
                    BufferedDuration = actualDuration + buffer,
                    IsCriticalPath = criticalPhases.Any(p => p.Id == phase.Id)
                });
            }
        }

        private static int CountWeekendDaysInRange(DateTime start, int days)
        {
            if (days <= 0) return 0;
            var weekendDays = 0;
            for (var offset = 0; offset < days; offset++)
            {
                var day = start.AddDays(offset).DayOfWeek;
                if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
                    weekendDays++;
            }
            return weekendDays;
        }
    }
}