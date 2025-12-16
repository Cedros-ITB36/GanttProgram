using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace GanttProgram
{
    public partial class PhaseDialog : Dialog
    {
        private readonly int _projectId;
        private Phase? _phase;
        private readonly bool _isEditMode;

        public PhaseDialog(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            _phase = new Phase();
            _isEditMode = false;
            PhaseAddDialog_Loaded();
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
        }

        public PhaseDialog(PhaseViewModel selectedPhaseView)
        {
            InitializeComponent();
            var phaseView = selectedPhaseView;
            _projectId = selectedPhaseView.ProjectId;
            _isEditMode = true;
            EditDialog_Loaded(phaseView);
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
        }

        private void PhaseAddDialog_Loaded()
        {
            Loaded += async (s, e) =>
            {
                await using var context = new GanttDbContext();
                var phases = await context.Phase
                    .Where(p => p.ProjectId == _projectId)
                    .ToListAsync();
                VorgaengerListBox.ItemsSource = phases;
                VorgaengerListBox.DisplayMemberPath = "Name";
            };
        }

        private void EditDialog_Loaded(PhaseViewModel phaseView)
        {
            Loaded += async (s, e) =>
            {
                await using var context = new GanttDbContext();

                var phaseEntity = await context.Phase
                    .Include(p => p.Predecessors)
                    .FirstOrDefaultAsync(p => p.Id == phaseView.Id);
                if (phaseEntity != null)
                    _phase = phaseEntity;

                var allPhases = await context.Phase
                    .Where(p => p.ProjectId == _projectId)
                    .Include(p => p.Predecessors)
                    .ToListAsync();

                var successorIds = GetAllSuccessorIds(phaseView.Id, allPhases);

                var phases = allPhases
                    .Where(p => p.Id != phaseView.Id && !successorIds.Contains(p.Id))
                    .ToList();

                VorgaengerListBox.ItemsSource = phases;
                VorgaengerListBox.DisplayMemberPath = "Name";

                var currentPredecessorIds = phaseEntity?.Predecessors.Select(v => v.PredecessorId).ToList() ?? [];

                foreach (var phase in phases.Where(phase => currentPredecessorIds.Contains(phase.Id)))
                {
                    VorgaengerListBox.SelectedItems.Add(phase);
                }

                NummerTextBox.Text = phaseView.Number;
                NameTextBox.Text = phaseView.Name;
                DauerTextBox.Text = phaseView.Duration?.ToString() ?? string.Empty;
            };
        }

        private static HashSet<int> GetAllSuccessorIds(int phaseId, List<Phase> allPhases)
        {
            var result = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(phaseId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                var directSuccessor = allPhases
                    .Where(p => p.Predecessors.Any(v => v.PredecessorId == currentId))
                    .Select(p => p.Id)
                    .ToList();

                foreach (var successorId in directSuccessor.Where(successorId => result.Add(successorId)))
                {
                    stack.Push(successorId);
                }
            }
            return result;
        }

        protected override async void SaveDialog(object? sender, RoutedEventArgs? e)
        {
            var number = NummerTextBox.Text;
            var name = NameTextBox.Text;
            var duration = string.IsNullOrWhiteSpace(DauerTextBox.Text) ? (int?)null : Convert.ToInt32(DauerTextBox.Text);
            var selectedPhases = VorgaengerListBox.SelectedItems.Cast<Phase>().ToList();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Der Name der Phase darf nicht leer sein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await using (var context = new GanttDbContext())
            {
                var exists = await context.Phase
                    .AnyAsync(p =>
                        p.ProjectId == _projectId &&
                        (p.Number == number || p.Name == name) &&
                        (!_isEditMode || (_phase != null && p.Id != _phase.Id))
                    );

                if (exists)
                {
                    MessageBox.Show("Es existiert bereits eine Phase mit dieser Nummer oder diesem Namen im Projekt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (await CheckIfCriticalPathDurationIsLongerThanProjectDuration(duration, selectedPhases))
            {
                return;
            }

            if (!_isEditMode)
            {
                _phase = new Phase
                {
                    Predecessors = new List<Predecessor>(),
                    Successors = new List<Predecessor>(),
                    ProjectId = _projectId,
                    Number = number,
                    Name = name,
                    Duration = duration
                };

                await using var context = new GanttDbContext();
                context.Phase.Add(_phase);
                await context.SaveChangesAsync();

                foreach (var predecessor in selectedPhases)
                {
                    context.Predecessor.Add(new Predecessor
                    {
                        PhaseId = _phase.Id,
                        PredecessorId = predecessor.Id
                    });
                }
                await context.SaveChangesAsync();
            }
            else
            {
                await using (var context = new GanttDbContext())
                {
                    var oldPredecessors = context.Predecessor.Where(v => _phase != null && v.PhaseId == _phase.Id);
                    context.Predecessor.RemoveRange(oldPredecessors);
                    await context.SaveChangesAsync();
                }

                await using (var context = new GanttDbContext())
                {
                    var phaseToUpdate = await context.Phase.FirstOrDefaultAsync(p => _phase != null && p.Id == _phase.Id);
                    if (phaseToUpdate != null)
                    {
                        phaseToUpdate.Number = number;
                        phaseToUpdate.Name = name;
                        phaseToUpdate.Duration = duration;

                        foreach (var predecessor in selectedPhases)
                        {
                            context.Predecessor.Add(new Predecessor
                            {
                                PhaseId = phaseToUpdate.Id,
                                PredecessorId = predecessor.Id
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }

            DialogResult = true;
            Close();
        }

        private async Task<bool> CheckIfCriticalPathDurationIsLongerThanProjectDuration(int? dauer, List<Phase> selectedVorgaenger)
        {
            Project? project;
            List<Phase> projectPhases;
            await using (var context = new GanttDbContext())
            {
                project = (await context.Project
                    .Include(p => p.Phases)
                    .ThenInclude(p => p.Predecessors)
                    .FirstOrDefaultAsync(p => p.Id == _projectId));

                if (project == null)
                {
                    MessageBox.Show("Projekt nicht gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }

                if (project.StartDate == null || project.EndDate == null)
                {
                    return false;
                }
                projectPhases = project.Phases.ToList();
            }

            if (_isEditMode)
            {
                var phaseToEdit = projectPhases.FirstOrDefault(p => _phase != null && p.Id == _phase.Id);
                if (phaseToEdit != null)
                {
                    phaseToEdit.Duration = dauer;
                    phaseToEdit.Predecessors = selectedVorgaenger
                        .Select(vp => new Predecessor { PhaseId = phaseToEdit.Id, PredecessorId = vp.Id })
                        .ToList();
                }
            }
            else
            {
                var newPhase = new Phase
                { 
                    Number = NummerTextBox.Text,
                    Name = NameTextBox.Text,
                    Duration = dauer,
                    ProjectId = _projectId,
                    Predecessors = selectedVorgaenger
                        .Select(vp => new Predecessor { PhaseId = -1, PredecessorId = vp.Id })
                        .ToList()
                };
                projectPhases.Add(newPhase);
            }

            var criticalDuration = CriticalPathHelper.GetCriticalPathDuration(projectPhases);

            if (project.StartDate == null || project.EndDate == null)
            {
                MessageBox.Show("Das Projekt hat kein gültiges Start- oder Enddatum.", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            var projectDuration = CriticalPathHelper.CalculateWorkingDays(project.StartDate.Value, project.EndDate.Value);

            if (criticalDuration <= projectDuration) return false;
            MessageBox.Show($"Die Dauer des kritischen Pfads  ({criticalDuration} Tage) überschreitet die Projektdauer ({projectDuration} Tage).", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;

        }
    }
}
