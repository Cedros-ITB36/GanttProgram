namespace GanttProgram.Infrastructure
{
    public class Predecessor
    {
        public int PhaseId { get; set; }
        public int PredecessorId { get; set; }

        public Phase? Phase { get; set; }
        public Phase? PredecessorPhase { get; set; }
    }
}
