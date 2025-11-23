using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GanttProgram
{
    /// <summary>
    /// all Employee-related methods
    /// </summary>
    public partial class MainWindow
    {
        private async void OpenAddEmployeeDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new EmployeeDialog();
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadEmployeesAsync();
            }
        }

        private async void OpenEditEmployeeDialog(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is not Employee selectedEmployee) return;
            var dialog = new EmployeeDialog(selectedEmployee);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                await LoadEmployeesAsync();
            }
        }

        private async void DeleteEmployeePopup(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is not Employee selectedEmployee) return;
            var result = MessageBox.Show("Wollen Sie diesen Mitarbeiter wirklich löschen?",
                "Mitarbeiter löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            await using (var context = new GanttDbContext())
            {
                var managedProjects = await context.Project
                    .Where(p => p.EmployeeId == selectedEmployee.Id)
                    .ToListAsync();

                foreach (var project in managedProjects)
                {
                    project.EmployeeId = null;
                }

                await context.SaveChangesAsync();

                context.Employee.Attach(selectedEmployee);
                context.Employee.Remove(selectedEmployee);
                await context.SaveChangesAsync();
            }

            await LoadEmployeesAsync();
            await LoadProjectsAsync();
        }

        private async void ImportEmployeeCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true) return;
            var lines = await File.ReadAllLinesAsync(dialog.FileName, Encoding.UTF8);
            if (lines.Length < 2)
            {
                MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var header = lines[0].Split(';');
            var lastNameIdx = Array.IndexOf(header, "Name");
            var firstNameIdx = Array.IndexOf(header, "Vorname");
            var departmentIdx = Array.IndexOf(header, "Abteilung");
            var phoneIdx = Array.IndexOf(header, "Telefon");

            if (lastNameIdx == -1)
            {
                MessageBox.Show("Die Spalte 'Name' wurde nicht gefunden.", "Namen Spalte fehlt!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newEmployees = new List<Employee>();

            await using var context = new GanttDbContext();
            var existingNames = context.Employee.Select(m => m.LastName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(';');
                if (fields.Length < header.Length) continue;

                var name = fields[lastNameIdx];
                if (string.IsNullOrWhiteSpace(name) || existingNames.Contains(name))
                    continue;

                var employee = new Employee
                {
                    LastName = fields[lastNameIdx],
                    FirstName = firstNameIdx >= 0 ? fields[firstNameIdx] : null,
                    Department = departmentIdx >= 0 ? fields[departmentIdx] : null,
                    Phone = phoneIdx >= 0 ? fields[phoneIdx] : null
                };
                newEmployees.Add(employee);
                existingNames.Add(name);
            }

            if (newEmployees.Count > 0)
            {
                context.Employee.AddRange(newEmployees);
                await context.SaveChangesAsync();
                await LoadEmployeesAsync();
                MessageBox.Show("Import abgeschlossen.", "Import abgeschlossen.",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Keine neuen eindeutigen Mitarbeiter zum Import gefunden.", "Import abgebrochen",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportEmployeeCsv_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Keine Daten für Export vorhanden!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = "Mitarbeiter.csv"
            };

            if (dialog.ShowDialog() != true) return;
            var sb = new StringBuilder();

            var columns = EmployeeDataGrid.Columns
                .Where(c => c.Header?.ToString() != "Projekte")
                .ToList();

            for (var i = 0; i < columns.Count; i++)
            {
                sb.Append(columns[i].Header);
                if (i < columns.Count - 1)
                    sb.Append(';');
            }

            sb.AppendLine();

            foreach (var item in EmployeeDataGrid.Items)
            {
                if (item is null || item.GetType().Namespace == "System.Data.DataRowView") continue;
                for (var i = 0; i < columns.Count; i++)
                {
                    var cellContent = columns[i].GetCellContent(item);
                    var value = cellContent is TextBlock tb ? tb.Text : "";
                    sb.Append(value.Replace(";", ","));
                    if (i < columns.Count - 1)
                        sb.Append(';');
                }

                sb.AppendLine();
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Export abgeschlossen.", "Export abegschlossen.",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}