using Microsoft.EntityFrameworkCore;

namespace GanttProgram.Infrastructure
{
    public partial class GanttDbContext : DbContext
    {
        public virtual DbSet<Employee> Employee { get; set; }
        public virtual DbSet<Project> Project { get; set; }
        public virtual DbSet<Phase> Phase { get; set; }
        public virtual DbSet<Predecessor> Predecessor { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(@"Data Source=..\..\..\..\projektverwaltung.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LastName)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.FirstName)
                    .HasMaxLength(100)
                    .IsRequired(false);
                entity.Property(e => e.Department)
                    .HasMaxLength(100)
                    .IsRequired(false);
                entity.Property(e => e.Phone)
                    .HasMaxLength(50)
                    .IsRequired(false);

                entity.HasMany(m => m.Projects)
                    .WithOne(pr => pr.Employee)
                    .HasForeignKey(pr => pr.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.StartDate)
                    .IsRequired(false);
                entity.Property(e => e.EndDate)
                    .IsRequired(false);

                entity.HasMany(pr => pr.Phases)
                    .WithOne(ph => ph.Project)
                    .HasForeignKey(ph => ph.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Phase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Number)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.Duration)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Predecessor>(entity =>
            {
                entity.HasKey(e => new { e.PhaseId, e.PredecessorId });

                entity.HasOne(e => e.Phase)
                    .WithMany(o => o.Predecessors)
                    .HasForeignKey(e => e.PhaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.HasOne(e => e.PredecessorPhase)
                    .WithMany(o => o.Successors)
                    .HasForeignKey(e => e.PredecessorId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
        }
    }
}
