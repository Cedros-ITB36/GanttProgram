using System.Windows;
using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GanttProgram.Helper
{
    public static class GanttHelper
    {
        public static void ShowGanttChartForProject(int projectId)
        {
            using var context = new GanttDbContext();

            var project = context.Project
                .Include(pr => pr.Phases)
                .ThenInclude(ph => ph.Predecessors)
                .Include(pr => pr.Employee)
                .SingleOrDefault(pr => pr.Id == projectId);

            if (project == null)
            {
                MessageBox.Show("Das ausgewählte Projekt konnte nicht geladen werden.");
                return;
            }

            new GanttChartWindow(project).Show();
        }
    }
}