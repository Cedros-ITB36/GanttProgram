using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace GanttProgram
{
    /// <summary>
    /// all Project-related methods
    /// </summary>
    public partial class MainWindow
    {
        private async void OpenAddProjectDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new ProjectDialog(new ObservableCollection<Employee>(_employeeList));
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadProjectsAsync();
            }
        }

        private async void OpenEditProjectDialog(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is not ProjectViewModel selectedProjectView) return;
            var dialog = new ProjectDialog(selectedProjectView, new ObservableCollection<Employee>(_employeeList));
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await LoadProjectsAsync();
            }
        }

        private async void DeleteProjectPopup(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is not ProjectViewModel selectedProjectView) return;
            var result = MessageBox.Show("Wollen Sie dieses Projekt wirklich löschen?", "Projekt löschen",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            await using (var context = new GanttDbContext())
            {
                var project = context.Project
                    .Include(p => p.Phases)
                    .FirstOrDefault(p => p.Id == selectedProjectView.Project.Id);
                
                if (project == null) return;
                
                var phaseIds = project.Phases.Select(p => p.Id).ToList();
                var predecessors = context.Predecessor.Where(pr => phaseIds.Contains(pr.PhaseId) || phaseIds.Contains(pr.PredecessorId));
 
                context.Predecessor.RemoveRange(predecessors);
                context.Phase.RemoveRange(project.Phases);
                context.Project.Remove(project);
                await context.SaveChangesAsync();
            }
            await LoadProjectsAsync();
        }

        private async void ImportProjectJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON-Datei (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true) return;
            
            var lines = await File.ReadAllTextAsync(dialog.FileName, Encoding.UTF8);
            if (lines.Length == 0)
            {
                MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var importCount = JsonHelper.ImportProjects(lines);

            if (importCount == 0)
            {
                MessageBox.Show("Keine neuen eindeutigen Projekte zum Import gefunden.", "Import abgebrochen",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            await LoadProjectsAsync();
            await LoadEmployeesAsync();
            MessageBox.Show($"Es wurden {importCount} Projekte importiert.", "Import abgeschlossen.",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportProjectJson_Click(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Keine Daten zum Exportieren vorhanden!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON-Datei (*.json)|*.json",
                FileName = "Projekte.json"
            };

            if (dialog.ShowDialog() != true) return;
            
            var json = JsonHelper.SerializeProjects();
            File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
            
            MessageBox.Show("Export abgeschlossen.", "Export abgeschlossen.",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenPhasesForSelectedProject(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is not ProjectViewModel selectedProjectView) return;
            var phasesWindow = new PhasesWindow(selectedProjectView.Id);
            phasesWindow.Show();
        }

        private void GenerateGanttChart(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjectViewModel selectedProject)
            {
                GanttHelper.ShowGanttChartForProject(selectedProject.Id);
            }
        }
    }
}