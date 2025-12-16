using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using GanttProgram.Helper;

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

        private async void ImportEmployeeJson_Click(object sender, RoutedEventArgs e)
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

            var importCount = JsonHelper.ImportEmployees(lines);
            
            if (importCount == 0)
            {
                MessageBox.Show("Keine neuen eindeutigen Mitarbeiter zum Import gefunden.", "Import abgebrochen",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            await LoadEmployeesAsync();
            MessageBox.Show($"Es wurden {importCount} Mitarbeiter importiert.", "Import abgeschlossen.",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportEmployeeJson_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Keine Daten zum Exportieren vorhanden!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON-Datei (*.json)|*.json",
                FileName = "Mitarbeiter.json"
            };

            if (dialog.ShowDialog() != true) return;
            
            var json = JsonHelper.SerializeEmployees();
            File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
            
            MessageBox.Show("Export abgeschlossen.", "Export abgeschlossen.",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}