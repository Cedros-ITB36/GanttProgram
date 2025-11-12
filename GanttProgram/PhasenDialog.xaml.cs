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
        private readonly Phase _phase;
        private readonly bool _isEditMode;

        public PhasenDialog(Phase selectedPhase)
        {
            InitializeComponent();
            _phase = selectedPhase;
            _isEditMode = true;
            Loaded += PhaseEditDialog_Loaded;
        }
        public PhasenDialog(int projektId)
        {
            InitializeComponent();
            _projektId = projektId;
            _phase = new Phase();
            _isEditMode = false;
        }

        private void PhaseEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NummerTextBox.Text = _phase.Nummer.ToString();
            NameTextBox.Text = _phase.Name;
            DauerTextBox.Text = _phase.Dauer.ToString() ?? string.Empty;
            //VorgaengerTextBox.Text = _phase.Vorgaenger.ToString() ?? string.Empty;
        }


        private async void SavePhase(object sender, RoutedEventArgs e)
        {
            _phase.Nummer = NummerTextBox.Text;
            _phase.Name = NameTextBox.Text;
            _phase.Dauer = string.IsNullOrWhiteSpace(DauerTextBox.Text)
                ? null
                : Convert.ToInt32(DauerTextBox.Text);
            /*_phase.Vorgaenger = string.IsNullOrWhiteSpace(VorgaengerTextBox.Text)
                ? null
                : Convert.ToInt32(VorgaengerTextBox.Text);*/

            if (!_isEditMode)
            {
                _phase.ProjektId = _projektId;
            }

            using (var context = new GanttDbContext())
            {
                if (_isEditMode)
                {
                    context.Phase.Attach(_phase);
                    context.Entry(_phase).State = EntityState.Modified;
                }
                else
                {
                    context.Phase.Add(_phase);
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
