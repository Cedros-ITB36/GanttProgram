namespace GanttProgram.Infrastructure
{
    public class Phase
    {
        public int Id { get; init; }
        public string Nummer { get; set; }
        public string Name { get; set; }
        public int? Dauer { get; set; }
        public int ProjektId { get; set; }

        public Projekt Projekt { get; set; }
        public ICollection<Vorgaenger> Vorgaenger { get; set; } = [];
        public ICollection<Vorgaenger> Nachfolger { get; set; } = [];

        public override string ToString()
        {
            return $"{Nummer}: {Name}";
        }
    }
}
