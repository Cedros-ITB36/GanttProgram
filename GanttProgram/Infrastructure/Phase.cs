namespace GanttProgram.Infrastructure
{
    public class Phase
    {
        public int Id { get; init; }
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Duration { get; set; }
        public int ProjectId { get; set; }

        public Project? Project { get; set; }
        public ICollection<Predecessor> Predecessors { get; set; } = [];
        public ICollection<Predecessor> Successors { get; set; } = [];

        public override string ToString()
        {
            return $"{Number}: {Name}";
        }
    }
}
