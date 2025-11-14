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
    /// Interaktionslogik für MitarbeiterDialog.xaml
    /// </summary>
    public partial class MitarbeiterDialog : Window
    {
        private readonly Mitarbeiter _mitarbeiter;
        private readonly bool _isEditMode;

        public MitarbeiterDialog(Mitarbeiter selectedMitarbeiter)
        {
            InitializeComponent();
            _mitarbeiter = selectedMitarbeiter;
            _isEditMode = true;
            Loaded += MitarbeiterEditDialog_Loaded;
        }
        public MitarbeiterDialog()
        {
            InitializeComponent();
            _mitarbeiter = new Mitarbeiter();
            _isEditMode = false;
        }

        private void MitarbeiterEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = _mitarbeiter.Name;
            VornameTextBox.Text = _mitarbeiter.Vorname ?? string.Empty;
            AbteilungTextBox.Text = _mitarbeiter.Abteilung ?? string.Empty;
            TelefonTextBox.Text = _mitarbeiter.Telefon ?? string.Empty;
        }

        private async void SaveMitarbeiter(object sender, RoutedEventArgs e)
        {
            _mitarbeiter.Name = NameTextBox.Text;
            _mitarbeiter.Vorname = VornameTextBox.Text;
            _mitarbeiter.Abteilung = AbteilungTextBox.Text;
            _mitarbeiter.Telefon = TelefonTextBox.Text;

            using (var context = new GanttDbContext())
            {
                bool exists = await context.Mitarbeiter
                    .AnyAsync(m => m.Name == _mitarbeiter.Name && m.Id != _mitarbeiter.Id);

                if (exists)
                {
                    MessageBox.Show("Ein Mitarbeiter mit diesem Namen existiert bereits.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_isEditMode)
                {
                    context.Mitarbeiter.Attach(_mitarbeiter);
                    context.Entry(_mitarbeiter).State = EntityState.Modified;
                }
                else
                {
                    context.Mitarbeiter.Add(_mitarbeiter);
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
