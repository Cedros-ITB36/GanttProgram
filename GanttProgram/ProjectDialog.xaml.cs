using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GanttProgram
{
    public partial class ProjectDialog
    {
        private readonly Project _project;
        private readonly string? _responsibleEmployee;
        private readonly bool _isEditMode;

        public ICommand? SaveCommand { get; }

        public ProjectDialog(ProjectViewModel selectedProjectView, ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            _project = selectedProjectView.Project;
            selectedProjectView.EmployeeList = employeeList;
            _responsibleEmployee = selectedProjectView.ResponsibleEmployee;
            DataContext = selectedProjectView;
            _isEditMode = true;
            Loaded += ProjectEditDialog_Loaded;
            SaveCommand = new RelayCommand(_ => SaveProject(null, null));
        }
        public ProjectDialog(ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            VerantwortlicherComboBox.ItemsSource = employeeList;
            _project = new Project();
            _isEditMode = false;
        }

        private void ProjectEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            BezeichnungTextBox.Text = _project.Title;
            StartdatumDatePickerBox.SelectedDate = _project.StartDate;
            EnddatumDatePickerBox.SelectedDate = _project.EndDate;
            VerantwortlicherComboBox.Text = _responsibleEmployee;
        }

        private async void SaveProject(object? sender, RoutedEventArgs? e)
        {
            _project.Title = BezeichnungTextBox.Text;
            _project.StartDate = StartdatumDatePickerBox.SelectedDate;
            _project.EndDate = EnddatumDatePickerBox.SelectedDate;

            if (_isEditMode)
            {
                var istZuKurz = await CheckProjectDatesAgainstCriticalPath(_project.Id, _project.StartDate, _project.EndDate);
                if (istZuKurz)
                {
                    return;
                }
            }

            await using (var context = new GanttDbContext())
            {
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

            var criticalDuration = CriticalPathHelper.GetCriticalPathDauer(projectPhases);

            if (newStartDate == null || newEndDate == null)
            {
                MessageBox.Show("Bitte gültige Start- und Enddaten angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            var projectDuration = CriticalPathHelper.CalculateWorkingDays(newStartDate.Value, newEndDate.Value);

            if (criticalDuration <= projectDuration) return false;
            MessageBox.Show($"Die Dauer des kritischen Pfads ({criticalDuration} Tage) überschreitet die Projektdauer ({projectDuration} Tage).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;

        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
