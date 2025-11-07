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
        }
        
        private async Task LoadMitarbeiterAsync()
        {
            using (var context = new GanttDbContext())
            {
                var mitarbeiterListe = await context.Mitarbeiter.ToListAsync();
                MitarbeiterDataGrid.ItemsSource = mitarbeiterListe;
            }
        }
    }
}
