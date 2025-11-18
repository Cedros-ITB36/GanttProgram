using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace GanttProgram
{
    /// <summary>
    /// Interaktionslogik für ProjektDialog.xaml
    /// </summary>
    public partial class ProjektDialog : Window
    {
        private readonly Projekt _projekt;
        private readonly string _verantwortlicher;
        private readonly ProjektViewModel _viewModel;
        private readonly bool _isEditMode;

        public ICommand SaveCommand { get; }

        public ProjektDialog(ProjektViewModel selectedProjektView, ObservableCollection<Mitarbeiter> mitarbeiterListe)
        {
            InitializeComponent();
            _projekt = selectedProjektView.Projekt;
            _viewModel = selectedProjektView;
            _viewModel.MitarbeiterListe = mitarbeiterListe;
            _verantwortlicher = selectedProjektView.Verantwortlicher;
            DataContext = _viewModel;
            _isEditMode = true;
            Loaded += ProjektEditDialog_Loaded;
            SaveCommand = new RelayCommand(_ => SaveProjekt(null, null));
        }
        public ProjektDialog(ObservableCollection<Mitarbeiter> mitarbeiterListe)
        {
            InitializeComponent();
            VerantwortlicherComboBox.ItemsSource = mitarbeiterListe;
            _projekt = new Projekt();
            _isEditMode = false;
        }

        private void ProjektEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            BezeichnungTextBox.Text = _projekt.Bezeichnung;
            StartdatumDatePickerBox.SelectedDate = _projekt.StartDatum;
            EnddatumDatePickerBox.SelectedDate = _projekt.EndDatum;
            VerantwortlicherComboBox.Text = _verantwortlicher;
        }

        private async void SaveProjekt(object sender, RoutedEventArgs e)
        {
            _projekt.Bezeichnung = BezeichnungTextBox.Text;
            _projekt.StartDatum = StartdatumDatePickerBox.SelectedDate;
            _projekt.EndDatum = EnddatumDatePickerBox.SelectedDate;

            if (_isEditMode)
            {
                bool istZuKurz = await CheckProjektTermineGegenKritischenPfad(_projekt.Id, _projekt.StartDatum, _projekt.EndDatum);
                if (istZuKurz)
                {
                    return;
                }
            }
            using (var context = new GanttDbContext())
            {
                if (_isEditMode)
                {
                    context.Projekt.Attach(_projekt);
                    context.Entry(_projekt).State = EntityState.Modified;
                }
                else
                {
                    context.Projekt.Add(_projekt);
                }

                await context.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        private async Task<bool> CheckProjektTermineGegenKritischenPfad(int projektId, DateTime? neuesStartDatum, DateTime? neuesEndDatum)
        {
            Projekt projekt;
            List<Phase> phasenImProjekt;
            using (var context = new GanttDbContext())
            {
                projekt = await context.Projekt
                    .Include(p => p.Phasen)
                    .ThenInclude(p => p.Vorgaenger)
                    .FirstOrDefaultAsync(p => p.Id == projektId);

                if (projekt == null)
                {
                    MessageBox.Show("Projekt nicht gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }

                phasenImProjekt = projekt.Phasen.ToList();
            }

            var kritischeDauer = CriticalPathHelper.GetCriticalPathDauer(phasenImProjekt);

            if (neuesStartDatum == null || neuesEndDatum == null)
            {
                MessageBox.Show("Bitte gültige Start- und Enddaten angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            int projektdauer = CriticalPathHelper.BerechneWerktage(neuesStartDatum.Value, neuesEndDatum.Value);

            if (kritischeDauer > projektdauer)
            {
                MessageBox.Show($"Die Dauer des kritischen Pfads ({kritischeDauer} Tage) überschreitet die Projektdauer ({projektdauer} Tage).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }

            return false;
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
