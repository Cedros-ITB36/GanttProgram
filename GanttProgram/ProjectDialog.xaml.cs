using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace GanttProgram
{
    public partial class ProjectDialog : Dialog
    {
        private Project _project;
        private readonly bool _isEditMode;
        
        public ProjectDialog(ProjectViewModel selectedProjectView, ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            _isEditMode = true;
            _project = null!;
            employeeList.Insert(0, new Employee { LastName = "Kein Verantwortlicher" });
            selectedProjectView.EmployeeList = employeeList;
            DataContext = selectedProjectView;
            EditDialog_Loaded(selectedProjectView, employeeList);
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
            Title = $"Projekt \"{selectedProjectView.Project}\" bearbeiten";
        }

        public ProjectDialog(ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            employeeList.Insert(0, new Employee { LastName = "Kein Verantwortlicher" });
            VerantwortlicherComboBox.ItemsSource = employeeList;
            _project = new Project();
            _isEditMode = false;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
            Title = "Neues Projekt erstellen";
        }

        protected void EditDialog_Loaded(ProjectViewModel selectedProjectView, ObservableCollection<Employee> employeeList)
        {
            Loaded += async (s, e) =>
            {
                await using var context = new GanttDbContext();
                var projectEntity = await context.Project
                    .Include(p => p.Employee)
                    .FirstOrDefaultAsync(p => p.Id == selectedProjectView.Project.Id);

                if (projectEntity != null)
                {
                    _project = projectEntity;
                    BezeichnungTextBox.Text = projectEntity.Title;
                    StartdatumDatePickerBox.SelectedDate = projectEntity.StartDate;
                    EnddatumDatePickerBox.SelectedDate = projectEntity.EndDate;
                    VerantwortlicherComboBox.SelectedItem = employeeList.FirstOrDefault(emp => emp.Id == projectEntity.EmployeeId)
                                                            ?? employeeList[0];
                }
            };
        }

        protected override async void SaveDialog(object? sender, RoutedEventArgs? e)
        {

            if (_isEditMode)
            {
                var istZuKurz = await CheckProjectDatesAgainstCriticalPath(_project.Id, _project.StartDate, _project.EndDate);
                if (istZuKurz)
                {
                    return;
                }
            }

            _project.Title = BezeichnungTextBox.Text.Trim();
            _project.StartDate = StartdatumDatePickerBox.SelectedDate;
            _project.EndDate = EnddatumDatePickerBox.SelectedDate;

            if (string.IsNullOrWhiteSpace(_project.Title))
            {
                MessageBox.Show("Bitte geben Sie eine Projektbezeichnung ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedEmployee = VerantwortlicherComboBox.SelectedItem as Employee;
            if (selectedEmployee != null && selectedEmployee.LastName == "Kein Verantwortlicher")
            {
                _project.EmployeeId = null;
                _project.Employee = null;
            }
            else
            {
                _project.EmployeeId = selectedEmployee?.Id;
                _project.Employee = selectedEmployee;
            }
            
            await using (var context = new GanttDbContext())
            {
                var exists = await context.Project
                    .AnyAsync(p => p.Title == _project.Title && p.Id != _project.Id);

                if (exists)
                {
                    MessageBox.Show("Ein Projekt mit dieser Beschreibung existiert bereits.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (_isEditMode)
                {
                    context.Project.Attach(_project);
                    context.Entry(_project).State = EntityState.Modified;
                }
                else
                {
                    context.Project.Add(_project);
                }

                await context.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        private static async Task<bool> CheckProjectDatesAgainstCriticalPath(int projectId, DateTime? newStartDate, DateTime? newEndDate)
        {
            Project? project;
            List<Phase> projectPhases;
            await using (var context = new GanttDbContext())
            {
                project = await context.Project
                    .Include(p => p.Phases)
                    .ThenInclude(p => p.Predecessors)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    MessageBox.Show("Projekt nicht gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }

                projectPhases = project.Phases.ToList();
            }

            if (newStartDate == null || newEndDate == null)
            {
                return false;
            }

            var criticalDuration = CriticalPathHelper.GetCriticalPathDuration(projectPhases);
            var projectDuration = CriticalPathHelper.CalculateWorkingDays(newStartDate.Value, newEndDate.Value);

            if (criticalDuration <= projectDuration) return false;
            MessageBox.Show($"Die Dauer des kritischen Pfads ({criticalDuration} Tage) überschreitet die Projektdauer ({projectDuration} Tage).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;

        }
    }
}
