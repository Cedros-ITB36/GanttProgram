namespace GanttProgram.Infrastructure
{
    public class Mitarbeiter
    {
        public int Id { get; init; }
        public string Name { get; set; }
        public string Vorname { get; set; }
        public string Abteilung { get; set; }
        public string? Telefon { get; set; }
    }
}
