using System.Text.Json;
using System.Text.Json.Serialization;
using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GanttProgram.Helper;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions ToJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private static readonly JsonSerializerOptions FromJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public static string ToJson<T>(IEnumerable<T> items) => JsonSerializer.Serialize(items, ToJsonOptions);

    public static List<T> FromJson<T>(string json) => JsonSerializer.Deserialize<List<T>>(json, FromJsonOptions) ?? [];

    public static string SerializeProjects()
    {
        using var context = new GanttDbContext();
        
        var projects = context.Project
            .Include(p => p.Employee)
            .Include(p => p.Phases)
            .ThenInclude(p => p.Predecessors)
            .AsNoTracking()
            .ToList();

        return ToJson(projects);
    }

    public static string SerializeEmployees()
    {
        using var context = new GanttDbContext();
        var employees = context.Employee.AsNoTracking().ToList();
        return ToJson(employees);
    }
    
    public static int ImportProjects(string json)
    {
        using var context = new GanttDbContext();
        
        var importedProjects = FromJson<Project>(json);
        var importCount = 0;

        foreach (var imported in importedProjects)
        {
            var project = context.Project
                .Include(p => p.Phases)
                .SingleOrDefault(p => p.Title == imported.Title);

            if (project != null) continue;

            if (imported.Employee != null)
            {
                var employee = context.Employee.SingleOrDefault(e => e.LastName == imported.Employee.LastName);
                if (employee is null)
                {
                    imported.Employee.Id = 0;
                    context.Employee.Add(imported.Employee);
                    context.SaveChanges();
                    imported.Employee = context.Employee.SingleOrDefault(e => e.LastName == imported.Employee.LastName);
                }
                else imported.Employee = employee;
            }

            var phaseIdMap = new Dictionary<int, Phase>();
            var predecessorSources = new Dictionary<int, List<Predecessor>>();

            foreach (var phase in imported.Phases)
            {
                var oldId = phase.Id;

                predecessorSources[oldId] = phase.Predecessors.ToList();

                phase.Id = 0;
                phase.ProjectId = 0;
                phase.Predecessors.Clear();

                phaseIdMap[oldId] = phase;
            }

            imported.Id = 0;
            context.Project.Add(imported);
            context.SaveChanges();
            ++importCount;

            foreach (var (oldPhaseId, predecessors) in predecessorSources)
            {
                var phase = phaseIdMap[oldPhaseId];

                foreach (var p in predecessors)
                {
                    context.Predecessor.Add(new Predecessor
                    {
                        PhaseId = phase.Id,
                        PredecessorId = phaseIdMap[p.PredecessorId].Id
                    });
                }
            }

            context.SaveChanges();
        }

        return importCount;
    }
    
    public static int ImportEmployees(string json)
    {
        using var context = new GanttDbContext();
        
        var importedEmployees = FromJson<Employee>(json);
        var importCount = 0;

        foreach (var imported in importedEmployees)
        {
            var employee = context.Employee.SingleOrDefault(e =>
                               e.LastName == imported.LastName);
            
            if (employee != null) continue;
            

            imported.Id = 0;
            context.Employee.Add(imported);
            context.SaveChanges();
            ++importCount;
        }

        return importCount;
    }
}