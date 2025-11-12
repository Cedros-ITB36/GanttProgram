using GanttProgram.Infrastructure;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Mitarbeiter> _mitarbeiterListe;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMitarbeiterAsync();
            await LoadProjektAsync();
        }

        #region LoadData
        private async Task LoadMitarbeiterAsync()
        {
            using (var context = new GanttDbContext())
            {
                _mitarbeiterListe = await context.Mitarbeiter.ToListAsync();
                MitarbeiterDataGrid.ItemsSource = _mitarbeiterListe;
            }
        }

        private async Task LoadProjektAsync()
        {
            using (var context = new GanttDbContext())
            {
                var projektListe = await context.Projekt.ToListAsync();

                var projektAnzeigeListe = projektListe.Select(p =>
                    new ProjektViewModel(p)
                    {
                        Verantwortlicher = _mitarbeiterListe.FirstOrDefault(m => m.Id == p.MitarbeiterId)?.Name
                    }
                ).ToList();

                ProjektDataGrid.ItemsSource = projektAnzeigeListe;
            }
        }
        #endregion


        #region MitarbeiterFunctions

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
            else
            {
                MessageBox.Show("Bitte wählen Sie einen Mitarbeiter aus.");
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
            else
            {
                MessageBox.Show("Bitte wählen Sie einen Mitarbeiter aus.");
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
                    MessageBox.Show("Die Datei enthält keine Daten.");
                    return;
                }

                var header = lines[0].Split(';');
                int nameIdx = Array.IndexOf(header, "Name");
                int vornameIdx = Array.IndexOf(header, "Vorname");
                int abteilungIdx = Array.IndexOf(header, "Abteilung");
                int telefonIdx = Array.IndexOf(header, "Telefon");

                if (nameIdx == -1)
                {
                    MessageBox.Show("Die Spalte 'Name' wurde nicht gefunden.");
                    return;
                }

                var neueMitarbeiter = new List<Mitarbeiter>();

                for (int i = 1; i < lines.Length; i++)
                {
                    var fields = lines[i].Split(';');
                    if (fields.Length < header.Length) continue;

                    var mitarbeiter = new Mitarbeiter
                    {
                        Name = fields[nameIdx],
                        Vorname = vornameIdx >= 0 ? fields[vornameIdx] : null,
                        Abteilung = abteilungIdx >= 0 ? fields[abteilungIdx] : null,
                        Telefon = telefonIdx >= 0 ? fields[telefonIdx] : null
                    };
                    neueMitarbeiter.Add(mitarbeiter);
                }

                using (var context = new GanttDbContext())
                {
                    context.Mitarbeiter.AddRange(neueMitarbeiter);
                    await context.SaveChangesAsync();
                }

                await LoadMitarbeiterAsync();
                MessageBox.Show("Import abgeschlossen.");
            }
        }

        private void ExportMitarbeiterCsv_Click(object sender, RoutedEventArgs e)
        {
            if (MitarbeiterDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.");
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
                MessageBox.Show("Export abgeschlossen.");
            }
        }

        #endregion

        #region ProjektFunctions
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
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.");
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
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.");
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
                    MessageBox.Show("Die Datei enthält keine Daten.");
                    return;
                }

                var header = lines[0].Split(';');
                int bezIdx = Array.IndexOf(header, "Bezeichnung");
                int startIdx = Array.IndexOf(header, "Startdatum");
                int endIdx = Array.IndexOf(header, "Enddatum");
                int verantwIdx = Array.IndexOf(header, "Verantwortlicher");

                if (bezIdx == -1 || startIdx == -1 || endIdx == -1)
                {
                    MessageBox.Show("Mindestens eine benötigte Spalte fehlt.");
                    return;
                }

                using (var context = new GanttDbContext())
                {
                    var mitarbeiter = context.Mitarbeiter.ToList();
                    var neueProjekte = new List<Projekt>();

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var fields = lines[i].Split(';');
                        if (fields.Length < header.Length) continue;

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
                        DateTime.TryParseExact(fields[startIdx], dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
                        DateTime.TryParseExact(fields[endIdx], dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out end);

                        var projekt = new Projekt
                        {
                            Bezeichnung = fields[bezIdx],
                            StartDatum = start,
                            EndDatum = end,
                            MitarbeiterId = mitarbeiterId
                        };
                        neueProjekte.Add(projekt);
                    }

                    context.Projekt.AddRange(neueProjekte);
                    await context.SaveChangesAsync();
                }

                await LoadProjektAsync();
                MessageBox.Show("Import abgeschlossen.");
            }
        }

        private void ExportProjektCsv_Click(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.");
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
                MessageBox.Show("Export abgeschlossen.");
            }
        }

        private void OpenPhasesForSelectedProject(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is ProjektViewModel selectedProjektView)
            {
                var phasenWindow = new PhasenWindow(selectedProjektView.Id);
                phasenWindow.Show();
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.");
            }
        }

        private void GenerateGanttChart(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is Projekt selectedProject)
            {
                using var context = new GanttDbContext();

                var project = context.Projekt
                    .Include(pr => pr.Phasen)
                        .ThenInclude(ph => ph.Vorgaenger)
                    .Include(pr => pr.Mitarbeiter)
                    .SingleOrDefault(pr => pr.Id == selectedProject.Id);

                if (project == null)
                {
                    MessageBox.Show("Das ausgewählte Projekt konnte nicht geladen werden.");
                    return;
                }

                new GanttChartWindow(project).Show();
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.");
            }
        }
        #endregion
    }
}
