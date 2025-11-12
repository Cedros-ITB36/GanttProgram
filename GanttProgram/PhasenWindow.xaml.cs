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

        private async void OpenAddPhaseDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new PhasenDialog(_projektId);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadPhasenAsync();
            }
        }

        private async void OpenEditPhaseDialog(object sender, RoutedEventArgs e)
        {
            if (PhasenDataGrid.SelectedItem is Phase selectedPhase)
            {
                var dialog = new PhasenDialog(selectedPhase);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    await LoadPhasenAsync();
                }
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie eine Phase aus.");
            }
        }

        private async void OpenDeletePhasePopup(object sender, RoutedEventArgs e)
        {
            if (PhasenDataGrid.SelectedItem is Phase selectedPhase)
            {
                var result = MessageBox.Show("Wollen Sie diese Phase wirklich löschen?",
                    "Phase löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new GanttDbContext())
                    {
                        context.Phase.Attach(selectedPhase);
                        context.Phase.Remove(selectedPhase);
                        await context.SaveChangesAsync();
                    }

                    await LoadPhasenAsync();
                }
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie eine Phase aus.");
            }
        }
    }
}
