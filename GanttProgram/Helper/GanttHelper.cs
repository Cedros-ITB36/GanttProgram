using GanttProgram.Infrastructure;
using GanttProgram.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;

namespace GanttProgram.Infrastructure
{
    public static class GanttHelper
    {
        public static void ShowGanttChartForProject(int projektId)
        {
            using var context = new GanttDbContext();

            var project = context.Projekt
                .Include(pr => pr.Phasen)
                .ThenInclude(ph => ph.Vorgaenger)
                .Include(pr => pr.Mitarbeiter)
                .SingleOrDefault(pr => pr.Id == projektId);

            if (project == null)
            {
                MessageBox.Show("Das ausgewählte Projekt konnte nicht geladen werden.");
                return;
            }

            new GanttChartWindow(project).Show();
        }
    }
}