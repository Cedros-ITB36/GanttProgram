using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
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

                var allePhasen = await context.Phase
                    .Where(p => p.ProjektId == _projektId)
                    .Include(p => p.Vorgaenger)
                    .ToListAsync();

                var nachfolgerIds = GetAllSuccessorIds(_phasenView.Id, allePhasen);

                var phasen = allePhasen
                    .Where(p => p.Id != _phasenView.Id && !nachfolgerIds.Contains(p.Id))
                    .ToList();

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

        private static HashSet<int> GetAllSuccessorIds(int phaseId, List<Phase> allePhasen)
        {
            var result = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(phaseId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                var direkteNachfolger = allePhasen
                    .Where(p => p.Vorgaenger.Any(v => v.VorgaengerId == currentId))
                    .Select(p => p.Id)
                    .ToList();

                foreach (var nachfolgerId in direkteNachfolger)
                {
                    if (result.Add(nachfolgerId))
                        stack.Push(nachfolgerId);
                }
            }
            return result;
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

        private async void SavePhase(object sender, RoutedEventArgs e)
        {
            var nummer = NummerTextBox.Text;
            var name = NameTextBox.Text;
            int? dauer = string.IsNullOrWhiteSpace(DauerTextBox.Text) ? (int?)null : Convert.ToInt32(DauerTextBox.Text);
            var selectedPhasen = VorgaengerListBox.SelectedItems.Cast<Phase>().ToList();

            if (await CheckIfCriticalPathDurationIsLongerThanProjectDuration(dauer, selectedPhasen)) return;

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
                using (var context = new GanttDbContext())
                {
                    var alteVorgaenger = context.Vorgaenger.Where(v => v.PhasenId == _phase.Id);
                    context.Vorgaenger.RemoveRange(alteVorgaenger);
                    await context.SaveChangesAsync();
                }

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

        private async Task<bool> CheckIfCriticalPathDurationIsLongerThanProjectDuration(int? dauer, List<Phase> selectedVorgaenger)
        {
            Projekt projekt;
            List<Phase> phasenImProjekt;
            using (var context = new GanttDbContext())
            {
                projekt = await context.Projekt
                    .Include(p => p.Phasen)
                        .ThenInclude(p => p.Vorgaenger)
                    .FirstOrDefaultAsync(p => p.Id == _projektId);

                if (projekt == null)
                {
                    MessageBox.Show("Projekt nicht gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }

                phasenImProjekt = projekt.Phasen.ToList();
            }

            if (_isEditMode)
            {
                var phaseToEdit = phasenImProjekt.FirstOrDefault(p => p.Id == _phase.Id);
                if (phaseToEdit != null)
                {
                    phaseToEdit.Dauer = dauer;
                    phaseToEdit.Vorgaenger = selectedVorgaenger
                        .Select(vp => new Vorgaenger { PhasenId = phaseToEdit.Id, VorgaengerId = vp.Id })
                        .ToList();
                }
            }
            else
            {
                var neuePhase = new Phase
                { 
                    Nummer = NummerTextBox.Text,
                    Name = NameTextBox.Text,
                    Dauer = dauer,
                    ProjektId = _projektId,
                    Vorgaenger = selectedVorgaenger
                        .Select(vp => new Vorgaenger { PhasenId = -1, VorgaengerId = vp.Id })
                        .ToList()
                };
                phasenImProjekt.Add(neuePhase);
            }

            var kritischeDauer = CriticalPathHelper.GetCriticalPathDauer(phasenImProjekt);

            if (projekt.StartDatum == null || projekt.EndDatum == null)
            {
                MessageBox.Show("Das Projekt hat kein gültiges Start- oder Enddatum.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            int projektdauer = CriticalPathHelper.BerechneWerktage(projekt.StartDatum.Value, projekt.EndDatum.Value);
            
            if (kritischeDauer > projektdauer)
            {
                MessageBox.Show($"Die Dauer des kritischen Pfads  ({kritischeDauer} Tage) überschreitet die Projektdauer ({projektdauer} Tage).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
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
