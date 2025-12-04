using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace GanttProgram
{
    public partial class MainWindow
    {
        private List<Employee> _employeeList = [];

        public ICommand CloseCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            CloseCommand = new RelayCommand(_ => this.Close());
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEmployeesAsync();
            await LoadProjectsAsync();
        }

        public void ActivateProjectTab()
        {
            MainTabControl.SelectedIndex = 1;
        }

        private async Task LoadEmployeesAsync()
        {
            await using var context = new GanttDbContext();
            _employeeList = await context.Employee.ToListAsync();
            EmployeeDataGrid.ItemsSource = _employeeList;

            if (_employeeList.Count > 0)
            {
                EmployeeDataGrid.SelectedIndex = 0;
            }
        }

        private async Task LoadProjectsAsync()
        {
            await using var context = new GanttDbContext();
            var projectList = await context.Project.ToListAsync();

            var projectDisplayList = projectList.Select(p =>
                new ProjectViewModel(p)
                {
                    ResponsibleEmployee = _employeeList
                        .FirstOrDefault(m => m.Id == p.EmployeeId) is { } emp
                        ? $"{emp.LastName}, {emp.FirstName}"
                        : string.Empty
                }
            ).ToList();

            ProjektDataGrid.ItemsSource = projectDisplayList;

            if (projectDisplayList.Count > 0)
            {
                ProjektDataGrid.SelectedIndex = 0;
            }
        }
    }
}
