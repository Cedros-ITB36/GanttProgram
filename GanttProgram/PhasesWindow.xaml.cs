using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace GanttProgram
{
    public partial class PhasesWindow
    {
        private readonly int _projectId;

        public ICommand CloseCommand { get; }

        public PhasesWindow(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            Loaded += PhaseWindow_Loaded;
            Closing += Window_Closing;
            CloseCommand = new RelayCommand(_ => this.Close());
            var context = new GanttDbContext();
            Title = $"Phasen von \"{context.Project.Find(_projectId)}\"";
        }

        private async void PhaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPhasesAsync();
        }

        private static void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            if (mainWindow != null) return;
            mainWindow = new MainWindow();
            mainWindow.Show();
            mainWindow.ActivateProjectTab();
        }

        private async Task LoadPhasesAsync()
        {
            await using var context = new GanttDbContext();
            var phases = context.Phase
                .Where(p => p.ProjectId == _projectId)
                .Include(p => p.Predecessors)
                .ThenInclude(v => v.PredecessorPhase)
                .ToList();

            var phaseViewModels = phases.Select(p => new PhaseViewModel(p)).ToList();
            PhasenDataGrid.ItemsSource = phaseViewModels;

            if (phaseViewModels.Count > 0)
            {
                PhasenDataGrid.SelectedIndex = 0;
            }
        }

        private async void OpenAddPhaseDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new PhaseDialog(_projectId);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadPhasesAsync();
            }
        }

        private async void OpenEditPhaseDialog(object sender, RoutedEventArgs e)
        {
            if (PhasenDataGrid.SelectedItem is not PhaseViewModel selectedPhase) return;
            var dialog = new PhaseDialog(selectedPhase);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await LoadPhasesAsync();
            }
        }

        private async void DeletePhasePopup(object sender, RoutedEventArgs e)
        {
            if (PhasenDataGrid.SelectedItem is not PhaseViewModel selectedPhase) return;
            var result = MessageBox.Show("Wollen Sie diese Phase wirklich löschen?",
                "Phase löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            await using (var context = new GanttDbContext())
            {
                var predecessorEntries = context.Predecessor
                    .Where(v => v.PhaseId == selectedPhase.Id || v.PredecessorId == selectedPhase.Id);
                context.Predecessor.RemoveRange(predecessorEntries);

                var phase = await context.Phase.FindAsync(selectedPhase.Id);
                if (phase != null)
                {
                    context.Phase.Remove(phase);
                }

                await context.SaveChangesAsync();
            }

            await LoadPhasesAsync();

        }

        private void GenerateGanttChart(object sender, RoutedEventArgs e)
        {
            GanttHelper.ShowGanttChartForProject(_projectId);
        }
    }
}
