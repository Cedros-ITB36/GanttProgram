namespace GanttProgram.Infrastructure
{
    public class Mitarbeiter
    {
        public int Id { get; init; }
        public string Name { get; set; }
        public string? Vorname { get; set; }
        public string? Abteilung { get; set; }
        public string? Telefon { get; set; }

        public ICollection<Projekt> Projekte { get; set; } = [];

        public override string ToString()
        {
            if (Vorname == null)
                return Name;
            return $"{Name}, {Vorname}";
        }
    }
}
