using GanttProgram.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GanttProgram.ViewModels
{
    public class PhasenViewModel
    {
        public int Id { get; set; }
        public string Nummer { get; set; }
        public string Name { get; set; }
        public int? Dauer { get; set; }
        public int ProjektId { get; set; }
        public string VorgaengerListe { get; set; }

        public PhasenViewModel(Phase phase)
        {
            Id = phase.Id;
            Nummer = phase.Nummer;
            Name = phase.Name;
            Dauer = phase.Dauer;
            ProjektId = phase.ProjektId;
            VorgaengerListe = string.Join(", ",
                phase.Vorgaenger.Select(v => v.VorgaengerPhase != null ? v.VorgaengerPhase.Nummer : "?"));
        }
    }
}
