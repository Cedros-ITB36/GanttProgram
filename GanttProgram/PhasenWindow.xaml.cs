using GanttProgram.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace GanttProgram
{
    /// <summary>
    /// Interaktionslogik für PhasenWindow.xaml
    /// </summary>
    public partial class PhasenWindow : Window
    {
        private readonly int _projektId;

        public PhasenWindow(int projektId)
        {
            InitializeComponent();
            _projektId = projektId;
            Loaded += PhasenWindow_Loaded;
        }

        private async void PhasenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPhasenAsync();
        }

        private async Task LoadPhasenAsync()
        {
            using (var context = new GanttDbContext())
            {
                var phasenListe = await context.Phase
                    .Where(p => p.ProjektId == _projektId)
                    .ToListAsync();
                PhasenDataGrid.ItemsSource = phasenListe;
            }
        }

        private void OpenDeletePhasePopup(object sender, RoutedEventArgs e)
        {

        }

        private void OpenEditPhaseDialog(object sender, RoutedEventArgs e)
        {

        }

        private void OpenAddPhaseDialog(object sender, RoutedEventArgs e)
        {

        }
    }
}
