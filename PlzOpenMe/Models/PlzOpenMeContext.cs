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
                throw new NotImplementedException();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomUser>(entity =>
            {
                entity.ToTable("POM_Users");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("UUID for all users in PlzOpenMe");

                entity.Property(e => e.AgreedOn)
                    .HasColumnType("datetime")
                    .HasComment("Date Time when the user agreed");

                entity.Property(e => e.Created)
                    .HasColumnType("datetime")
                    .HasComment("Date and time of users first contact with the bot");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnType("varchar(128)")
                    .HasComment("Telegram user first name")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.HasAgreed).HasComment("Stores if the user has agreed with the TOS and Privacy Policy");

                entity.Property(e => e.IsBot).HasComment("Sets the bot status for a telegram user");

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasColumnType("varchar(70)")
                    .HasComment("User's language in Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnType("varchar(128)")
                    .HasComment("Telegram user last name")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.UserId)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram ID for the user");

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnType("varchar(64)")
                    .HasComment("Username in Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
