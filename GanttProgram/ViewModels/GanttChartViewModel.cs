using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace GanttProgram.ViewModels
{
    public class GanttPhaseViewModel
    {
        public required Phase Phase { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; set; }
        public required Brush Color { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double BufferedWidth { get; set; }
        public double Height { get; set; }
        public bool IsCriticalPath { get; init; }
        public int ActualDuration { get; init; }
        public int BufferedDuration { get; init; }

        public string ToolTip => $"Phase {Phase}\n" +
                                 $"Dauer: {Phase.Duration} {(Phase.Duration == 1 ? "Tag" : "Tage")}\n" +
                                 $"Beginn: {StartDate.ToShortDateString()}\n" +
                                 $"Ende: {EndDate.AddDays(-1).ToShortDateString()}\n" +
                                 $"Kritischer Pfad: {(IsCriticalPath ? "Ja" : "Nein")}";
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
            Brushes.Green,
            Brushes.Orange
        ];

        public Project Project { get; }
        public ObservableCollection<GanttPhaseViewModel> PhaseViewModels { get; } = [];

        public GanttChartViewModel(Project project)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));

            if (project.Phases.Count == 0 || project.StartDate == null)
                return;

            ComputePhaseStarts();
        }

        private void ComputePhaseStarts()
        {
            var projectStart = Project.StartDate!.Value;
            var phaseStarts = new Dictionary<Phase, DateTime>();
            var phaseEnds = new Dictionary<Phase, DateTime>();

            var phases = Project.Phases.ToList();
            var criticalPhases = CriticalPathHelper.GetCriticalPathPhases(Project.Id);

            var allCalculated = false;
            while (!allCalculated)
            {
                allCalculated = true;
                foreach (var phase in phases.Where(phase => !phaseStarts.ContainsKey(phase)))
                {
                    if (phase.Predecessors.Count == 0)
                    {
                        phaseStarts[phase] = projectStart;
                    }
                    else
                    {
                        if (!phase.Predecessors.All(v => v.PredecessorPhase != null && phaseEnds.ContainsKey(v.PredecessorPhase)))
                        {
                            allCalculated = false;
                            continue;
                        }
                        var maxEnd = phase.Predecessors
                            .Select(v => phaseEnds[v.PredecessorPhase!])
                            .Max();
                        phaseStarts[phase] = maxEnd;
                    }

                    var baseDuration = phase.Duration ?? 0;

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
                var successors = Project.Phases
                    .Where(p => p.Predecessors.Any(v => v.PredecessorPhase != null && v.PredecessorPhase.Id == phase.Id))
                    .ToList();

                if (successors.Count == 0)
                    continue;

                var minSuccessorStart = successors.Min(s => phaseStarts[s]);
                phaseLatestEnd[phase] = minSuccessorStart;
            }

            foreach (var phase in phases.OrderBy(p => phaseStarts[p]))
            {
                var actualDuration = (phaseEnds[phase] - phaseStarts[phase]).Days;
                var buffer = (phaseLatestEnd[phase] - phaseEnds[phase]).Days;

                PhaseViewModels.Add(new GanttPhaseViewModel
                {
                    Phase = phase,
                    StartDate = phaseStarts[phase],
                    EndDate = phaseEnds[phase],
                    Color = PhaseColors[PhaseViewModels.Count % PhaseColors.Length],
                    ActualDuration = actualDuration,
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
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    weekendDays++;
            }
            return weekendDays;
        }
    }
}