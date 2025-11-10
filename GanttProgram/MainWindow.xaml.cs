using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace GanttProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
                var mitarbeiterListe = await context.Mitarbeiter.ToListAsync();
                MitarbeiterDataGrid.ItemsSource = mitarbeiterListe;
            }
        }

        private async Task LoadProjektAsync()
        {
            using (var context = new GanttDbContext())
            {
                var projektListe = await context.Projekt.ToListAsync();

                ProjektDataGrid.ItemsSource = projektListe;
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
        #endregion

        #region ProjektFunctions
        private async void OpenAddProjektDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new ProjektDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadProjektAsync();
            }
        }

        private async void OpenEditProjektDialog(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is Projekt selectedProjekt)
            {
                var dialog = new ProjektDialog(selectedProjekt);
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
            if (ProjektDataGrid.SelectedItem is Projekt selectedProjekt)
            {
                var result = MessageBox.Show("Wollen Sie dieses Projekt wirklich löschen?",
                    "Projekt löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new GanttDbContext())
                    {
                        context.Projekt.Attach(selectedProjekt);
                        context.Projekt.Remove(selectedProjekt);
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

        private void OpenPhasesForSelectedProject(object sender, RoutedEventArgs e)
        {
            if (ProjektDataGrid.SelectedItem is Projekt selectedProjekt)
            {
                var phasenWindow = new PhasenWindow(selectedProjekt.Id);
                phasenWindow.Show();
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.");
            }
        }

        private void GenerateGanttChart(object sender, RoutedEventArgs e)
        {
            var ganttChartWindow = new GanttChartWindow();
            ganttChartWindow.Show();
        }
        #endregion
    }
}
