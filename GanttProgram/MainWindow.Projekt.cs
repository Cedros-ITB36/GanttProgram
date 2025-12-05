using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using GanttProgram.Helper;
using Microsoft.EntityFrameworkCore;

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
                context.Project.Attach(selectedProjectView.Project);
                context.Project.Remove(selectedProjectView.Project);
                await context.SaveChangesAsync();
            }

            await LoadProjectsAsync();
        }

        private async void ImportProjectCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON-Datei (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true) return;
            
            var lines = await File.ReadAllTextAsync(dialog.FileName, Encoding.UTF8);
            if (lines.Length == 0)
            {
                MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var readProjects = JsonHelper.FromJson<Project>(lines);

            await using var context = new GanttDbContext();
            var existingProjects = context.Project
                .Include(pr => pr.Phases)
                .ThenInclude(ph => ph.Predecessors)
                .ToList();
            
            var titles = new HashSet<string>(existingProjects.Select(p => p.Title));
            readProjects.RemoveAll(p => titles.Contains(p.Title));

            if (readProjects.Count > 0)
            {
                foreach (var project in readProjects)
                {
                    project.Id = 0;
                    foreach (var phase in project.Phases)
                    {
                        phase.ProjectId = 0;
                        phase.Id = 0;
                    }
                }
                foreach (var project in readProjects)
                    context.Project.Add(project);
                await context.SaveChangesAsync();
                await LoadProjectsAsync();
                MessageBox.Show("Import abgeschlossen.", "Import abgeschlossen.", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Keine neuen eindeutigen Projekte zum Import gefunden.", "Import abgebrochen",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportProjectCsv_Click(object sender, RoutedEventArgs e)
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

            using var context = new GanttDbContext();
            
            var projectList = context.Project
                .Include(pr => pr.Phases)
                .ThenInclude(ph => ph.Predecessors)
                .ThenInclude(pr => pr.Phase)
                .ToList();
            var projectJson = JsonHelper.ToJson(projectList);

            File.WriteAllText(dialog.FileName, projectJson, Encoding.UTF8);
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