using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

                context.Phase.RemoveRange(project.Phases);
                context.Project.Remove(project);
                await context.SaveChangesAsync();
            }

            await LoadProjectsAsync();
        }

        private async void ImportProjectCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true) return;
            var lines = await File.ReadAllLinesAsync(dialog.FileName, Encoding.UTF8);
            if (lines.Length < 2)
            {
                MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var header = lines[1].Split(';');
            var titleIdx = Array.IndexOf(header, "Bezeichnung");
            var startIdx = Array.IndexOf(header, "Startdatum");
            var endIdx = Array.IndexOf(header, "Enddatum");
            var responsibleIdx = Array.IndexOf(header, "Verantwortlicher");

            if (titleIdx == -1)
            {
                MessageBox.Show("Projektbezeichnung fehlt!", "Projektbezeichnung fehlt!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await using var context = new GanttDbContext();
            var employees = context.Employee.ToList();
            var existingProjectTitles = context.Project.Select(p => p.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newProjects = new List<Project>();

            for (var i = 2; i < lines.Length; i++)
            {
                var fields = lines[i].Split(';');
                if (fields.Length < header.Length) continue;

                var title = fields[titleIdx];
                if (string.IsNullOrWhiteSpace(title) || existingProjectTitles.Contains(title))
                    continue;

                int? employeeId = null;
                if (responsibleIdx >= 0 && !string.IsNullOrWhiteSpace(fields[responsibleIdx]))
                {
                    var name = fields[responsibleIdx];
                    var ma = employees.FirstOrDefault(m => m.LastName == name);
                    if (ma != null)
                        employeeId = ma.Id;
                }

                string[] dateFormats = ["MM/dd/yyyy", "dd.MM.yyyy", "yyyy-MM-dd"];
                DateTime.TryParseExact(fields[startIdx], dateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var start);
                DateTime.TryParseExact(fields[endIdx], dateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var end);

                var project = new Project
                {
                    Title = fields[titleIdx],
                    StartDate = start,
                    EndDate = end,
                    EmployeeId = employeeId
                };
                newProjects.Add(project);
                existingProjectTitles.Add(title);
            }

            if (newProjects.Count > 0)
            {
                context.Project.AddRange(newProjects);
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

        //TODO export Verantwortlicher
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
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = "Projekte.csv"
            };

            if (dialog.ShowDialog() != true) return;
            var sb = new StringBuilder();

            sb.AppendLine("Projekte:");
            var columns = ProjektDataGrid.Columns
                .Where(c => c.Header?.ToString() != "Mitarbeiter" && c.Header?.ToString() != "Verantwortlicher")
                .ToList();

            for (var i = 0; i < columns.Count; i++)
            {
                sb.Append(columns[i].Header);
                if (i < columns.Count - 1)
                    sb.Append(';');
            }
            sb.AppendLine();

            foreach (var item in ProjektDataGrid.Items)
            {
                if (item is null || item.GetType().Namespace == "System.Data.DataRowView") continue;
                for (var i = 0; i < columns.Count; i++)
                {
                    var cellContent = columns[i].GetCellContent(item);
                    var value = cellContent is TextBlock tb ? tb.Text : string.Empty;
                    sb.Append(value.Replace(";", ","));
                    if (i < columns.Count - 1)
                        sb.Append(';');
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("Phasen zu Projekten:");
            sb.AppendLine("ProjektId;PhasenId;Nummer;Name;Dauer;VorgaengerId");

            using (var context = new GanttDbContext())
            {
                var phases = context.Phase
                    .ToList();
                var predecessors = context.Predecessor.ToList();

                foreach (var phase in phases)
                {
                    sb.Append(phase.ProjectId);
                    sb.Append(';');
                    sb.Append(phase.Id);
                    sb.Append(';');
                    sb.Append(phase.Number?.Replace(";", ",") ?? "");
                    sb.Append(';');
                    sb.Append(phase.Name?.Replace(";", ",") ?? "");
                    sb.Append(';');
                    sb.Append(phase.Duration?.ToString() ?? "");
                    sb.Append(';');

                    var predecessorIds = predecessors
                        .Where(v => v.PhaseId == phase.Id)
                        .Select(v => v.PredecessorId.ToString())
                        .ToList();

                    sb.Append(string.Join(",", predecessorIds));
                    sb.AppendLine();
                }
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
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