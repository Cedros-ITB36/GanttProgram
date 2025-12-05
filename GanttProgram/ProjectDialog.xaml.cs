using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GanttProgram
{
    public partial class ProjectDialog : Dialog
    {
        private readonly Project _project;
        private readonly string? _responsibleEmployee;
        private readonly bool _isEditMode;
        
        public ProjectDialog(ProjectViewModel selectedProjectView, ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            _project = selectedProjectView.Project;
            selectedProjectView.EmployeeList = employeeList;
            _responsibleEmployee = selectedProjectView.ResponsibleEmployee;
            DataContext = selectedProjectView;
            _isEditMode = true;
            Loaded += EditDialog_Loaded;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
        }

        public ProjectDialog(ObservableCollection<Employee> employeeList)
        {
            InitializeComponent();
            VerantwortlicherComboBox.ItemsSource = employeeList;
            _project = new Project();
            _isEditMode = false;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
        }

        protected override void EditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            BezeichnungTextBox.Text = _project.Title;
            StartdatumDatePickerBox.SelectedDate = _project.StartDate;
            EnddatumDatePickerBox.SelectedDate = _project.EndDate;
            VerantwortlicherComboBox.Text = _responsibleEmployee;
        }

        protected override async void SaveDialog(object? sender, RoutedEventArgs? e)
        {
            _project.Title = BezeichnungTextBox.Text;
            _project.StartDate = StartdatumDatePickerBox.SelectedDate;
            _project.EndDate = EnddatumDatePickerBox.SelectedDate;

            if (string.IsNullOrWhiteSpace(_project.Title))
            {
                MessageBox.Show("Bitte geben Sie eine Projektbezeichnung ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

            var criticalDuration = CriticalPathHelper.GetCriticalPathDuration(projectPhases);

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
    }
}
