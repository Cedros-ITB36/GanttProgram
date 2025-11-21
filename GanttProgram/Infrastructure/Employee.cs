namespace GanttProgram.Infrastructure
{
    public class Employee
    {
        public int Id { get; init; }
        public string LastName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }

        public ICollection<Project> Projects { get; set; } = [];

        public override string ToString()
        {
            return FirstName == null ? LastName : $"{LastName}, {FirstName}";
        }
    }
}
