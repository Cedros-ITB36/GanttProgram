using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using GanttProgram.Infrastructure;
using Microsoft.Win32;

namespace GanttProgram
{
    /// <summary>
    /// all Mitarbeiter related methods
    /// </summary>
    public partial class MainWindow
    {
        private async void OpenAddMitarbeiterDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new MitarbeiterDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadMitarbeiterAsync();
            }
        }

        private async void OpenEditMitarbeiterDialog(object sender, RoutedEventArgs e)
        {
            if (MitarbeiterDataGrid.SelectedItem is Mitarbeiter selectedMitarbeiter)
            {
                var dialog = new MitarbeiterDialog(selectedMitarbeiter);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    await LoadMitarbeiterAsync();
                }
            }
        }

        private async void DeleteMitarbeiterPopup(object sender, RoutedEventArgs e)
        {
            if (MitarbeiterDataGrid.SelectedItem is Mitarbeiter selectedMitarbeiter)
            {
                var result = MessageBox.Show("Wollen Sie diesen Mitarbeiter wirklich löschen?",
                    "Mitarbeiter löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new GanttDbContext())
                    {
                        context.Mitarbeiter.Attach(selectedMitarbeiter);
                        context.Mitarbeiter.Remove(selectedMitarbeiter);
                        await context.SaveChangesAsync();
                    }

                    await LoadMitarbeiterAsync();
                }
            }
        }

        private async void ImportMitarbeiterCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var header = lines[0].Split(';');
                int nameIdx = Array.IndexOf(header, "Name");
                int vornameIdx = Array.IndexOf(header, "Vorname");
                int abteilungIdx = Array.IndexOf(header, "Abteilung");
                int telefonIdx = Array.IndexOf(header, "Telefon");

                if (nameIdx == -1)
                {
                    MessageBox.Show("Die Spalte 'Name' wurde nicht gefunden.", "Namen Spalte fehlt!", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var neueMitarbeiter = new List<Mitarbeiter>();

                using (var context = new GanttDbContext())
                {
                    var vorhandeneNamen = context.Mitarbeiter.Select(m => m.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var fields = lines[i].Split(';');
                        if (fields.Length < header.Length) continue;

                        var name = fields[nameIdx];
                        if (string.IsNullOrWhiteSpace(name) || vorhandeneNamen.Contains(name))
                            continue;

                        var mitarbeiter = new Mitarbeiter
                        {
                            Name = fields[nameIdx],
                            Vorname = vornameIdx >= 0 ? fields[vornameIdx] : null,
                            Abteilung = abteilungIdx >= 0 ? fields[abteilungIdx] : null,
                            Telefon = telefonIdx >= 0 ? fields[telefonIdx] : null
                        };
                        neueMitarbeiter.Add(mitarbeiter);
                        vorhandeneNamen.Add(name);
                    }

                    if (neueMitarbeiter.Count > 0)
                    {
                        context.Mitarbeiter.AddRange(neueMitarbeiter);
                        await context.SaveChangesAsync();
                        await LoadMitarbeiterAsync();
                        MessageBox.Show("Import abgeschlossen.", "Import abgeschlossen.", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Keine neuen eindeutigen Mitarbeiter zum Import gefunden.", "Import abgebrochen", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void ExportMitarbeiterCsv_Click(object sender, RoutedEventArgs e)
        {
            if (MitarbeiterDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Keine Daten für Export vorhanden!", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = "Mitarbeiter.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();

                var columns = MitarbeiterDataGrid.Columns
                    .Where(c => c.Header?.ToString() != "Projekte")
                    .ToList();

                for (int i = 0; i < columns.Count; i++)
                {
                    sb.Append(columns[i].Header);
                    if (i < columns.Count - 1)
                        sb.Append(";");
                }

                sb.AppendLine();

                foreach (var item in MitarbeiterDataGrid.Items)
                {
                    if (item is not null && item.GetType().Namespace != "System.Data.DataRowView")
                    {
                        for (int i = 0; i < columns.Count; i++)
                        {
                            var cellContent = columns[i].GetCellContent(item);
                            var value = cellContent is TextBlock tb ? tb.Text : "";
                            sb.Append(value.Replace(";", ","));
                            if (i < columns.Count - 1)
                                sb.Append(";");
                        }

                        sb.AppendLine();
                    }
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Export abgeschlossen.", "Export abegschlossen.", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}