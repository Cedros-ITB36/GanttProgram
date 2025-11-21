using GanttProgram.Infrastructure;

namespace GanttProgram.ViewModels
{
    public class PhaseViewModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public int? Duration { get; set; }
        public int ProjectId { get; set; }
        public string PredecessorList { get; set; }

        public PhaseViewModel(Phase phase)
        {
            Id = phase.Id;
            Number = phase.Number;
            Name = phase.Name;
            Duration = phase.Duration;
            ProjectId = phase.ProjectId;
            PredecessorList = string.Join(", ",
                phase.Predecessors.Select(v => v.PredecessorPhase?.Number));
        }
    }
}
