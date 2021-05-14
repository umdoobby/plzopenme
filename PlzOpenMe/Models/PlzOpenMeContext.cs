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

        public virtual DbSet<PomFile> PomFiles { get; set; }
        public virtual DbSet<PomUser> PomUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new NotImplementedException("SQL not configured");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomFile>(entity =>
            {
                entity.ToTable("POM_Files");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("ID for every file");

                entity.Property(e => e.CheckSum)
                    .IsRequired()
                    .HasColumnType("longtext")
                    .HasComment("File hash")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Link)
                    .IsRequired()
                    .HasColumnType("varchar(10)")
                    .HasComment("Unique URL id")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Message)
                    .HasColumnType("mediumtext")
                    .HasComment("Message attached to the file")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("mediumtext")
                    .HasComment("File name as report by Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.OnDisk)
                    .IsRequired()
                    .HasColumnType("varchar(25)")
                    .HasComment("Name of the file on the server")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.PostedBy)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram ID of who uploaded the file to Telegram");

                entity.Property(e => e.RemovalReason)
                    .HasColumnType("mediumtext")
                    .HasComment("comment on why file was removed")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.RemovedOn)
                    .HasColumnType("datetime")
                    .HasComment("Date time of file removal");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnType("enum('Animation','Audio','Document','Video','VideoNote','Voice','File','Sticker')")
                    .HasComment("File type as reported from Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.UploadedBy)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram user ID of who sent the file to the bot");

                entity.Property(e => e.UploadedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()")
                    .HasComment("Datetime when the file was uploaded");

                entity.Property(e => e.Views)
                    .HasColumnType("int(11)")
                    .HasComment("Number of times this file has been requested");

                entity.Property(e => e.VirusScannedOn)
                    .HasColumnType("datetime")
                    .HasComment("Date time when virus scan was finished");
            });

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
