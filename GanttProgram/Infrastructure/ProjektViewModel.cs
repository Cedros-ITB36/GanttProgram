using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GanttProgram.Infrastructure
{
    public class ProjektViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Mitarbeiter> MitarbeiterListe { get; set; } = new();
        public Projekt Projekt { get; set; }
        public string Verantwortlicher { get; set; } = string.Empty;

        public ProjektViewModel()
        {
            Projekt = new Projekt();
        }

        public ProjektViewModel(Projekt projekt)
        {
            Projekt = projekt ?? new Projekt();
        }

        public int Id => Projekt.Id;

        public string Bezeichnung
        {
            get => Projekt.Bezeichnung;
            set
            {
                if (Projekt != null && Projekt.Bezeichnung != value)
                {
                    Projekt.Bezeichnung = value;
                    OnPropertyChanged(nameof(Bezeichnung));
                }
            }
        }

        public DateTime? StartDatum
        {
            get => Projekt.StartDatum;
            set
            {
                if (Projekt != null && Projekt.StartDatum != value)
                {
                    Projekt.StartDatum = value;
                    OnPropertyChanged(nameof(StartDatum));
                }
            }
        }

        public DateTime? EndDatum
        {
            get => Projekt.EndDatum;
            set
            {
                if (Projekt != null && Projekt.EndDatum != value)
                {
                    Projekt.EndDatum = value;
                    OnPropertyChanged(nameof(EndDatum));
                }
            }
        }

        public int? MitarbeiterId
        {
            get => Projekt.MitarbeiterId;
            set
            {
                if (Projekt != null && Projekt.MitarbeiterId != value)
                {
                    Projekt.MitarbeiterId = value;
                    OnPropertyChanged(nameof(MitarbeiterId));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

