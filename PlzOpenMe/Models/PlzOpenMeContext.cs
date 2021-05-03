using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PlzOpenMeContext : DbContext
    {
        public PlzOpenMeContext()
        {
        }

        public PlzOpenMeContext(DbContextOptions<PlzOpenMeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PomUser> PomUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new NotImplementedException("Database context is not configured.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomUser>(entity =>
            {
                entity.ToTable("POM_Users");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("POM user primary key");

                entity.Property(e => e.Agreed).HasComment("POM user TOS agreement status");

                entity.Property(e => e.AgreedOn)
                    .HasColumnType("datetime")
                    .HasComment("POM date of agreed to TOS");

                entity.Property(e => e.Banned).HasComment("POM user ban status");

                entity.Property(e => e.BannedOn)
                    .HasColumnType("datetime")
                    .HasComment("POM date of ban");

                entity.Property(e => e.CreatedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()")
                    .HasComment("POM date of first contact");

                entity.Property(e => e.UserId)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram user ID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
