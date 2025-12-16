using System.Text.Json.Serialization;

namespace GanttProgram.Infrastructure;

public class Predecessor
{
    public int PhaseId { get; set; }
    public int PredecessorId { get; set; }

    [JsonIgnore] public Phase? Phase { get; set; }
    [JsonIgnore] public Phase? PredecessorPhase { get; set; }
}
