namespace GanttProgram.Infrastructure
{
    public class Phase
    {
        public int Id { get; init; }
        public string Nummer { get; set; }
        public string Name { get; set; }
        public int Dauer { get; set; }
        public int Vorgaenger { get; set; }
        public int ProjektId { get; set; }
    }
}
