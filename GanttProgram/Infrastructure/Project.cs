namespace GanttProgram.Infrastructure
{
    public class Project
    {
        public int Id {  get; init; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? EmployeeId { get; set; }

        public Employee? Employee { get; set; }
        public ICollection<Phase> Phases { get; set; } = [];

        public override string ToString()
        {
            return Title;
        }
    }
}
