using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GanttProgram.Helper
{
    public static class CriticalPathHelper
    {
        internal static List<Phase> GetCriticalPathPhases(int projectId)
        {
            List<Phase> projectPhases;
            using (var context = new GanttDbContext())
            {
                projectPhases = context.Phase
                    .Where(p => p.ProjectId == projectId)
                    .Include(p => p.Predecessors)
                    .ThenInclude(v => v.PredecessorPhase)
                    .ToList();
            }

            var criticalPathPhases = new List<Phase>();
            var endPhases = projectPhases
                .Where(p => !projectPhases.Any(x => x.Predecessors.Any(v => v.PredecessorId == p.Id)))
                .ToList();

            Phase? endPhaseCritical = null;

            if (endPhases.Count > 1)
            {
                var maxPathDuration = 0;
                foreach (var endPhase in endPhases)
                {
                    var pathDuration = BackwardsCalculatePathDuration(endPhase, projectPhases);
                    if (pathDuration <= maxPathDuration) continue;
                    maxPathDuration = pathDuration;
                    endPhaseCritical = endPhase;
                }
            }
            else
            {
                endPhaseCritical = endPhases.FirstOrDefault();
            }

            var current = endPhaseCritical;
            while (current != null)
            {
                criticalPathPhases.Insert(0, current);

                var predecessorPhases = projectPhases
                    .Where(p => current.Predecessors.Any(v => v.PredecessorId == p.Id))
                    .ToList();

                if (predecessorPhases.Count == 0)
                    break;

                Phase? next = null;
                var maxDuration = 0;
                foreach (var v in predecessorPhases)
                {
                    var duration = BackwardsCalculatePathDuration(v, projectPhases);
                    if (duration <= maxDuration) continue;
                    maxDuration = duration;
                    next = v;
                }
                current = next;
            }

            return criticalPathPhases;
        }

        internal static int GetCriticalPathDuration(List<Phase> projectPhases)
        {
            var endPhases = projectPhases
                .Where(p => !projectPhases.Any(x => x.Predecessors.Any(v => v.PredecessorId == p.Id)))
                .ToList();

            return endPhases.Select(endPhase => BackwardsCalculatePathDuration(endPhase, projectPhases)).Prepend(0).Max();
        }

        private static int BackwardsCalculatePathDuration(Phase phase, List<Phase> allPhases)
        {
            var predecessorPhases = allPhases
                .Where(p => phase.Predecessors.Any(v => v.PredecessorId == p.Id))
                .ToList();

            if (predecessorPhases.Count == 0)
            {
                return phase.Duration ?? 0;
            }

            var maxDuration = predecessorPhases.Select(v => BackwardsCalculatePathDuration(v, allPhases)).Prepend(0).Max();
            return (phase.Duration ?? 0) + maxDuration;
        }

        internal static int CalculateWorkingDays(DateTime start, DateTime end)
        {
            var workingDays = 0;
            for (var tag = start.Date; tag <= end.Date; tag = tag.AddDays(1))
            {
                if (tag.DayOfWeek != DayOfWeek.Saturday && tag.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }
    }
}
