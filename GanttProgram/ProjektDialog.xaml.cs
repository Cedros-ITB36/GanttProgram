using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
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

namespace GanttProgram
{
    /// <summary>
    /// Interaktionslogik für ProjektDialog.xaml
    /// </summary>
    public partial class ProjektDialog : Window
    {
        private readonly Projekt _projekt;
        private readonly bool _isEditMode;

        public ProjektDialog(Projekt selectedProjekt)
        {
            InitializeComponent();
            _projekt = selectedProjekt;
            _isEditMode = true;
            Loaded += ProjektEditDialog_Loaded;
        }
        public ProjektDialog()
        {
            InitializeComponent();
            _projekt = new Projekt();
            _isEditMode = false;
        }

        private void ProjektEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            BezeichnungTextBox.Text = _projekt.Bezeichnung;
            StartdatumDatePickerBox.SelectedDate = _projekt.StartDatum;
            EnddatumDatePickerBox.SelectedDate = _projekt.EndDatum;
            VerantwortlicherComboBox.Text = _projekt.MitarbeiterId?.ToString();

        }

        private async void SaveMitarbeiter(object sender, RoutedEventArgs e)
        {
            _projekt.Bezeichnung = BezeichnungTextBox.Text;
            _projekt.StartDatum = StartdatumDatePickerBox.SelectedDate;
            _projekt.EndDatum = EnddatumDatePickerBox.SelectedDate;
            //_projekt.MitarbeiterId? = VerantwortlicherComboBox.Text;

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

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
