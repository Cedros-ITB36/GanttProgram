using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GanttProgram.Helper
{
    public class CriticalPathHelper
    {
        internal static List<Phase> GetCriticalPathPhasen(int projectId)
        {
            List <Phase> phasenImProjekt;
            using (var context = new GanttDbContext())
            {
                phasenImProjekt = context.Phase
                    .Where(p => p.ProjektId == projectId)
                    .Include(p => p.Vorgaenger)
                    .ThenInclude(v => v.VorgaengerPhase)
                    .ToList();
            }

            var criticalPathPhasen = new List<Phase>();
            var endPhasen = phasenImProjekt
                .Where(p => !phasenImProjekt.Any(x => x.Vorgaenger != null && x.Vorgaenger.Any(v => v.VorgaengerId == p.Id)))
                .ToList();

            Phase endPhaseCritical = null;

            if (endPhasen.Count > 1)
            {
                int maxPfadDauer = 0;
                foreach (var endPhase in endPhasen)
                {
                    int pfadDauer = BackwardsCalculatePathDuration(endPhase, phasenImProjekt);
                    if (pfadDauer > maxPfadDauer)
                    {
                        maxPfadDauer = pfadDauer;
                        endPhaseCritical = endPhase;
                    }
                }
            }
            else
            {
                endPhaseCritical = endPhasen.FirstOrDefault();
            }

            var current = endPhaseCritical;
            while (current != null)
            {
                criticalPathPhasen.Insert(0, current);

                var vorgaengerPhasen = phasenImProjekt
                    .Where(p => current.Vorgaenger != null && current.Vorgaenger.Any(v => v.VorgaengerId == p.Id))
                    .ToList();

                if (!vorgaengerPhasen.Any())
                    break;

                Phase next = null;
                int maxDauer = 0;
                foreach (var v in vorgaengerPhasen)
                {
                    int dauer = BackwardsCalculatePathDuration(v, phasenImProjekt);
                    if (dauer > maxDauer)
                    {
                        maxDauer = dauer;
                        next = v;
                    }
                }
                current = next;
            }

            return criticalPathPhasen;
        }

        internal static int GetCriticalPathDauer(List<Phase> phasenImProjekt)
        {
            var endPhasen = phasenImProjekt
                .Where(p => !phasenImProjekt.Any(x => x.Vorgaenger != null && x.Vorgaenger.Any(v => v.VorgaengerId == p.Id)))
                .ToList();

            int maxPfadDauer = 0;
            foreach (var endPhase in endPhasen)
            {
                int pfadDauer = BackwardsCalculatePathDuration(endPhase, phasenImProjekt);
                if (pfadDauer > maxPfadDauer)
                    maxPfadDauer = pfadDauer;
            }
            return maxPfadDauer;
        }

        private static int BackwardsCalculatePathDuration(Phase phase, List<Phase> allePhasen)
        {
            var vorgaengerPhasen = allePhasen
                .Where(p => phase.Vorgaenger != null && phase.Vorgaenger.Any(v => v.VorgaengerId == p.Id))
                .ToList();

            if (!vorgaengerPhasen.Any())
            {
                return phase.Dauer ?? 0;
            }

            int maxDauer = 0;
            foreach (var v in vorgaengerPhasen)
            {
                int dauer = BackwardsCalculatePathDuration(v, allePhasen);
                if (dauer > maxDauer)
                {
                    maxDauer = dauer;
                }
            }
            return (phase.Dauer ?? 0) + maxDauer;
        }

        internal static int BerechneWerktage(DateTime start, DateTime ende)
        {
            int werktage = 0;
            for (var tag = start.Date; tag <= ende.Date; tag = tag.AddDays(1))
            {
                if (tag.DayOfWeek != DayOfWeek.Saturday && tag.DayOfWeek != DayOfWeek.Sunday)
                {
                    werktage++;
                }
            }
            return werktage;
        }
    }
}
