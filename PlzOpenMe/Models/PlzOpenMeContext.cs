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
        public virtual DbSet<PomLink> PomLinks { get; set; }
        public virtual DbSet<PomUser> PomUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new NotImplementedException("Database context not set");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomFile>(entity =>
            {
                entity.ToTable("POM_Files");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM unique ID");

                entity.Property(e => e.DeletedOn)
                    .HasColumnType("datetime")
                    .HasComment("When the file was deleted");

                entity.Property(e => e.FileId)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasComment("Telegram file ID")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.FileUniqueId)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasComment("Telegram unique file ID")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasComment("Name on disk")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Mime)
                    .HasColumnType("text")
                    .HasComment("Telegram MIME type")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Size)
                    .HasColumnType("int(11)")
                    .HasComment("Telegram file size");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnType("tinytext")
                    .HasComment("POM file type")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.UploadedOn)
                    .HasColumnType("datetime")
                    .HasComment("When file was received");
            });

            modelBuilder.Entity<PomLink>(entity =>
            {
                entity.ToTable("POM_Links");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM unique ID");

                entity.Property(e => e.AddedOn)
                    .HasColumnType("datetime")
                    .HasComment("When the link was created");

                entity.Property(e => e.Collection)
                    .HasColumnType("varchar(15)")
                    .HasComment("POM link to group of files")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.File)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM file ID");

                entity.Property(e => e.Link)
                    .IsRequired()
                    .HasColumnType("varchar(15)")
                    .HasComment("POM link to file")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasComment("Telegram name of file")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.RemovedOn)
                    .HasColumnType("datetime")
                    .HasComment("When the link was removed");

                entity.Property(e => e.UserId)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram user ID");

                entity.Property(e => e.Views)
                    .HasColumnType("bigint(20)")
                    .HasComment("Number of times the file has been requested");
            });

            modelBuilder.Entity<PomUser>(entity =>
            {
                entity.ToTable("POM_Users");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
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
