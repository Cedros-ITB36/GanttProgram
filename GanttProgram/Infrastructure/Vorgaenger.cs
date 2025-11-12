using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GanttProgram.Infrastructure
{
    public class Vorgaenger
    {
        public int PhasenId { get; set; }
        public int VorgaengerId { get; set; }

        public Phase Phase { get; set; }
        public Phase VorgaengerPhase { get; set; }
    }
}
