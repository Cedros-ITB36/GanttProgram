using System.Collections.ObjectModel;
using System.ComponentModel;
using GanttProgram.Infrastructure;

namespace GanttProgram.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Employee> EmployeeList { get; set; } = [];
        public Project Project { get; set; }
        public string ResponsibleEmployee { get; init; } = string.Empty;

        public ProjectViewModel(Project project)
        {
            Project = project;
        }

        public int Id => Project.Id;

        public string Title
        {
            get => Project.Title;
            set
            {
                if (Project.Title == value) return;
                Project.Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public DateTime? StartDate
        {
            get => Project.StartDate;
            set
            {
                if (Project.StartDate == value) return;
                Project.StartDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }

        public DateTime? EndDate
        {
            get => Project.EndDate;
            set
            {
                if (Project.EndDate == value) return;
                Project.EndDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }

        public int? EmployeeId
        {
            get => Project.EmployeeId;
            set
            {
                if (Project.EmployeeId == value) return;
                Project.EmployeeId = value;
                OnPropertyChanged(nameof(EmployeeId));
            }
        }

        protected void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

