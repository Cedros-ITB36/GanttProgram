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
    /// Interaktionslogik für PhasenDialog.xaml
    /// </summary>
    public partial class PhasenDialog : Window
    {
        private readonly int _projektId;
        private Phase _phase;
        private readonly PhasenViewModel _phasenView;
        private readonly bool _isEditMode;

        public PhasenDialog(PhasenViewModel selectedPhasenView)
        {
            InitializeComponent();
            _phasenView = selectedPhasenView;
            _projektId = selectedPhasenView.ProjektId;
            _isEditMode = true;

            Loaded += async (s, e) =>
            {
                using var context = new GanttDbContext();

                var phaseEntity = await context.Phase
                    .Include(p => p.Vorgaenger)
                    .FirstOrDefaultAsync(p => p.Id == _phasenView.Id);
                if (phaseEntity != null)
                    _phase = phaseEntity;

                var phasen = await context.Phase
                    .Where(p => p.ProjektId == _projektId && p.Id != _phasenView.Id)
                    .ToListAsync();

                VorgaengerListBox.ItemsSource = phasen;
                VorgaengerListBox.DisplayMemberPath = "Name";

                var aktuelleVorgaengerIds = phaseEntity?.Vorgaenger.Select(v => v.VorgaengerId).ToList() ?? new List<int>();

                foreach (var phase in phasen)
                {
                    if (aktuelleVorgaengerIds.Contains(phase.Id))
                        VorgaengerListBox.SelectedItems.Add(phase);
                }

                NummerTextBox.Text = _phasenView.Nummer;
                NameTextBox.Text = _phasenView.Name;
                DauerTextBox.Text = _phasenView.Dauer?.ToString() ?? string.Empty;
            };
        }
        public PhasenDialog(int projektId)
        {
            InitializeComponent();
            _projektId = projektId;
            _phase = new Phase();
            _isEditMode = false;

            Loaded += async (s, e) =>
            {
                using var context = new GanttDbContext();
                var phasen = await context.Phase
                    .Where(p => p.ProjektId == _projektId)
                    .ToListAsync();
                VorgaengerListBox.ItemsSource = phasen;
                VorgaengerListBox.DisplayMemberPath = "Name";
            };
        }

        private void PhaseEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NummerTextBox.Text = _phasenView.Nummer.ToString();
            NameTextBox.Text = _phasenView.Name;
            DauerTextBox.Text = _phasenView.Dauer.ToString() ?? string.Empty;
        }

        private async void SavePhase(object sender, RoutedEventArgs e)
        {
            var nummer = NummerTextBox.Text;
            var name = NameTextBox.Text;
            int? dauer = string.IsNullOrWhiteSpace(DauerTextBox.Text) ? (int?)null : Convert.ToInt32(DauerTextBox.Text);
            var selectedPhasen = VorgaengerListBox.SelectedItems.Cast<Phase>().ToList();

            if (!_isEditMode)
            {
                _phase = new Phase
                {
                    Vorgaenger = new List<Vorgaenger>(),
                    Nachfolger = new List<Vorgaenger>(),
                    ProjektId = _projektId,
                    Nummer = nummer,
                    Name = name,
                    Dauer = dauer
                };

                using (var context = new GanttDbContext())
                {
                    context.Phase.Add(_phase);
                    await context.SaveChangesAsync();

                    // Jetzt die Vorgänger anlegen, da _phase.Id erst nach SaveChanges gesetzt ist
                    foreach (var vorgaenger in selectedPhasen)
                    {
                        context.Vorgaenger.Add(new Vorgaenger
                        {
                            PhasenId = _phase.Id,
                            VorgaengerId = vorgaenger.Id
                        });
                    }
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                // 1. Vorgänger in separatem Kontext entfernen
                using (var context = new GanttDbContext())
                {
                    var alteVorgaenger = context.Vorgaenger.Where(v => v.PhasenId == _phase.Id);
                    context.Vorgaenger.RemoveRange(alteVorgaenger);
                    await context.SaveChangesAsync();
                }

                // 2. Phase aktualisieren und neue Vorgänger hinzufügen
                using (var context = new GanttDbContext())
                {
                    var phaseToUpdate = await context.Phase.FirstOrDefaultAsync(p => p.Id == _phase.Id);
                    if (phaseToUpdate != null)
                    {
                        phaseToUpdate.Nummer = nummer;
                        phaseToUpdate.Name = name;
                        phaseToUpdate.Dauer = dauer;

                        foreach (var vorgaenger in selectedPhasen)
                        {
                            context.Vorgaenger.Add(new Vorgaenger
                            {
                                PhasenId = phaseToUpdate.Id,
                                VorgaengerId = vorgaenger.Id
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }
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
