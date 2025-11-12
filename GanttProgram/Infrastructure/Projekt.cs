namespace GanttProgram.Infrastructure
{
    public class Projekt
    {
        public int Id {  get; init; }
        public string Bezeichnung { get; set; }
        public DateTime? StartDatum { get; set; }
        public DateTime? EndDatum { get; set; }
        public int? MitarbeiterId { get; set; }
    }
}
