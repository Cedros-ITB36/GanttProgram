using Microsoft.EntityFrameworkCore;

namespace GanttProgram.Infrastructure
{
    public partial class GanttDbContext : DbContext
    {
        public virtual DbSet<Mitarbeiter> Mitarbeiter { get; set; }
        public virtual DbSet<Projekt> Projekt { get; set; }
        public virtual DbSet<Phase> Phase { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(@"Data Source=C:\Users\cmunsch\source\repos\GanttProgram\projektverwaltung.db");
                // TODO: Richtigen Pfad einfügen und evtl erstellen, wenn nicht vorhanden
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mitarbeiter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Vorname)
                    .HasMaxLength(100)
                    .IsRequired(false);
                entity.Property(e => e.Abteilung)
                    .HasMaxLength(100)
                    .IsRequired(false);
                entity.Property(e => e.Telefon)
                    .HasMaxLength(50)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Projekt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Bezeichnung)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.StartDatum)
                    .IsRequired(false);
                entity.Property(e => e.EndDatum)
                    .IsRequired(false);

                entity.HasOne<Mitarbeiter>()
                    .WithMany()
                    .HasForeignKey(e => e.MitarbeiterId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Phase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nummer)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.Dauer)
                    .IsRequired(false);

                entity.HasOne<Phase>()
                    .WithMany()
                    .HasForeignKey(e => e.Vorgaenger)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne<Projekt>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjektId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
        }
    }
}
