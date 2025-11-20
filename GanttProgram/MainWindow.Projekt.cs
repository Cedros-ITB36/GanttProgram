using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GanttProgram
{
    /// <summary>
    /// all Projekt related methods
    /// </summary>
    public partial class MainWindow
    {
        private async void OpenAddProjektDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new ProjektDialog(new ObservableCollection<Mitarbeiter>(_mitarbeiterListe));
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadProjektAsync();
            }
        }

        private async void OpenEditProjektDialog(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjektViewModel selectedProjektView)
            {
                var dialog = new ProjektDialog(selectedProjektView, new ObservableCollection<Mitarbeiter>(_mitarbeiterListe));
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    await LoadProjektAsync();
                }
            }
        }

        private async void OpenDeleteProjektPopup(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjektViewModel selectedProjektView)
            {
                var result = MessageBox.Show("Wollen Sie dieses Projekt wirklich löschen?",
                    "Projekt löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new GanttDbContext())
                    {
                        context.Projekt.Attach(selectedProjektView.Projekt);
                        context.Projekt.Remove(selectedProjektView.Projekt);
                        await context.SaveChangesAsync();
                    }

                    await LoadProjektAsync();
                }
            }
        }

        private async void ImportProjektCsv_Click(object sender, RoutedEventArgs e)
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
                    MessageBox.Show("Die Datei enthält keine Daten.", "Keine Daten in der Importdatei!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var header = lines[0].Split(';');
                int bezIdx = Array.IndexOf(header, "Bezeichnung");
                int startIdx = Array.IndexOf(header, "Startdatum");
                int endIdx = Array.IndexOf(header, "Enddatum");
                int verantwIdx = Array.IndexOf(header, "Verantwortlicher");

                if (bezIdx == -1)
                {
                    MessageBox.Show("Projektbezeichnung fehlt!", "Projektbezeichnung fehlt!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new GanttDbContext())
                {
                    var mitarbeiter = context.Mitarbeiter.ToList();
                    var vorhandeneBezeichnungen = context.Projekt.Select(p => p.Bezeichnung)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var neueProjekte = new List<Projekt>();

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var fields = lines[i].Split(';');
                        if (fields.Length < header.Length) continue;

                        var bezeichnung = fields[bezIdx];
                        if (string.IsNullOrWhiteSpace(bezeichnung) || vorhandeneBezeichnungen.Contains(bezeichnung))
                            continue;

                        int? mitarbeiterId = null;
                        if (verantwIdx >= 0 && !string.IsNullOrWhiteSpace(fields[verantwIdx]))
                        {
                            var name = fields[verantwIdx];
                            var ma = mitarbeiter.FirstOrDefault(m => m.Name == name);
                            if (ma != null)
                                mitarbeiterId = ma.Id;
                        }

                        DateTime start, end;
                        string[] dateFormats = { "MM/dd/yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
                        DateTime.TryParseExact(fields[startIdx], dateFormats, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out start);
                        DateTime.TryParseExact(fields[endIdx], dateFormats, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out end);

                        var projekt = new Projekt
                        {
                            Bezeichnung = fields[bezIdx],
                            StartDatum = start,
                            EndDatum = end,
                            MitarbeiterId = mitarbeiterId
                        };
                        neueProjekte.Add(projekt);
                        vorhandeneBezeichnungen.Add(bezeichnung);
                    }

                    if (neueProjekte.Count > 0)
                    {
                        context.Projekt.AddRange(neueProjekte);
                        await context.SaveChangesAsync();
                        await LoadProjektAsync();
                        MessageBox.Show("Import abgeschlossen.", "Import abgeschlossen.", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Keine neuen eindeutigen Projekte zum Import gefunden.", "Import abgebrochen",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

            }
        }

        private void ExportProjektCsv_Click(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Keine Daten zum Exportieren vorhanden!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = "Projekte.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();

                sb.AppendLine("Projekte:");
                var columns = ProjektDataGrid.Columns
                    .Where(c => c.Header?.ToString() != "Mitarbeiter" && c.Header?.ToString() != "Verantwortlicher")
                    .ToList();

                for (int i = 0; i < columns.Count; i++)
                {
                    sb.Append(columns[i].Header);
                    if (i < columns.Count - 1)
                        sb.Append(";");
                }
                sb.AppendLine();

                foreach (var item in ProjektDataGrid.Items)
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
                sb.AppendLine();

                sb.AppendLine("Phasen zu Projekten:");
                sb.AppendLine("ProjektId;PhasenId;Nummer;Name;Dauer;VorgaengerId");

                using (var context = new GanttDbContext())
                {
                    var phasen = context.Phase
                        .ToList();
                    var vorgaengerBeziehungen = context.Vorgaenger.ToList();

                    foreach (var phase in phasen)
                    {
                        sb.Append(phase.ProjektId);
                        sb.Append(";");
                        sb.Append(phase.Id);
                        sb.Append(";");
                        sb.Append(phase.Nummer?.Replace(";", ",") ?? "");
                        sb.Append(";");
                        sb.Append(phase.Name?.Replace(";", ",") ?? "");
                        sb.Append(";");
                        sb.Append(phase.Dauer?.ToString() ?? "");
                        sb.Append(";");

                        var vorgaengerIds = vorgaengerBeziehungen
                            .Where(v => v.PhasenId == phase.Id)
                            .Select(v => v.VorgaengerId.ToString())
                            .ToList();

                        sb.Append(string.Join(",", vorgaengerIds));
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Export abgeschlossen.", "Export abgeschlossen.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenPhasesForSelectedProject(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjektViewModel selectedProjektView)
            {
                var phasenWindow = new PhasenWindow(selectedProjektView.Id);
                phasenWindow.Show();
            }
        }

        internal void GenerateGanttChart(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjektViewModel selectedProject)
            {
                GanttHelper.ShowGanttChartForProject(selectedProject.Id);
            }
        }
    }
}