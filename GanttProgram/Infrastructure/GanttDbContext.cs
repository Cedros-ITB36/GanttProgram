using Microsoft.EntityFrameworkCore;
using System.IO;

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
                optionsBuilder.UseSqlite(@"Data Source=..\..\..\..\projektverwaltung.db");
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

                entity.HasMany(m => m.Projekte)
                    .WithOne(pr => pr.Mitarbeiter)
                    .HasForeignKey(pr => pr.MitarbeiterId)
                    .OnDelete(DeleteBehavior.SetNull);
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

                entity.HasMany(pr => pr.Phasen)
                    .WithOne(ph => ph.Projekt)
                    .HasForeignKey(ph => ph.ProjektId)
                    .OnDelete(DeleteBehavior.Cascade);
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

                entity.HasMany(ph => ph.Vorgaenger)
                    .WithOne(vp => vp.Phase)
                    .HasForeignKey(vp => vp.PhasenId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(ph => ph.Nachfolger)
                    .WithOne(vp => vp.VorgaengerPhase)
                    .HasForeignKey(vp => vp.VorgaengerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Vorgaenger>(entity =>
            {
                entity.HasKey(e => new { e.PhasenId, e.VorgaengerId });

                entity.HasOne(e => e.Phase)
                    .WithMany(o => o.Vorgaenger)
                    .HasForeignKey(e => e.PhasenId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.HasOne(e => e.VorgaengerPhase)
                    .WithMany(o => o.Nachfolger)
                    .HasForeignKey(e => e.VorgaengerId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
        }
    }
}
