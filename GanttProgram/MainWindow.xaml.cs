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
using System.Windows.Media.Imaging;

namespace GanttProgram
{
    /// <summary>
    /// Loading and displaying Mitarbeiter and Projekt data
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

        public void ActivateProjectTab()
        {
            MainTabControl.SelectedIndex = 1;
        }

        private async Task LoadMitarbeiterAsync()
        {
            using (var context = new GanttDbContext())
            {
                _mitarbeiterListe = await context.Mitarbeiter.ToListAsync();
                MitarbeiterDataGrid.ItemsSource = _mitarbeiterListe;

                if (_mitarbeiterListe.Count > 0)
                {
                    MitarbeiterDataGrid.SelectedIndex = 0;
                }
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

                if (projektAnzeigeListe.Count > 0)
                {
                    ProjektDataGrid.SelectedIndex = 0;
                }
            }
        }
    }
}
