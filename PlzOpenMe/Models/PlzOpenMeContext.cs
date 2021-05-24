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

        public virtual DbSet<PomAnimation> PomAnimations { get; set; }
        public virtual DbSet<PomAudio> PomAudios { get; set; }
        public virtual DbSet<PomFile> PomFiles { get; set; }
        public virtual DbSet<PomLink> PomLinks { get; set; }
        public virtual DbSet<PomPhoto> PomPhotos { get; set; }
        public virtual DbSet<PomSticker> PomStickers { get; set; }
        public virtual DbSet<PomUser> PomUsers { get; set; }
        public virtual DbSet<PomVideo> PomVideos { get; set; }
        public virtual DbSet<PomVideoNote> PomVideoNotes { get; set; }
        public virtual DbSet<PomVoice> PomVoices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new NotImplementedException("Database connection not configured.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomAnimation>(entity =>
            {
                entity.ToTable("POM_Animations");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM animation ID");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasComment("Video length in seconds from Telegram");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of associated file");

                entity.Property(e => e.Height)
                    .HasColumnType("int(11)")
                    .HasComment("File height from Telegram");

                entity.Property(e => e.Width)
                    .HasColumnType("int(11)")
                    .HasComment("File width from Telegram");
            });

            modelBuilder.Entity<PomAudio>(entity =>
            {
                entity.ToTable("POM_Audio");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM audio ID");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasComment("Length in seconds of audio track from Telegram");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of associated file");

                entity.Property(e => e.Performer)
                    .HasColumnType("text")
                    .HasComment("Performer of the audio from Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Title)
                    .HasColumnType("text")
                    .HasComment("Title of the audio from Telegram")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

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

                entity.Property(e => e.File)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM file ID");

                entity.Property(e => e.Link)
                    .IsRequired()
                    .HasColumnType("varchar(15)")
                    .HasComment("POM link to file")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_bin");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("mediumtext")
                    .HasComment("Telegram name of file")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_bin");

                entity.Property(e => e.RemovedOn)
                    .HasColumnType("datetime")
                    .HasComment("When the link was removed");

                entity.Property(e => e.Thumbnail)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM file ID of thumbnail");

                entity.Property(e => e.UserId)
                    .HasColumnType("bigint(20)")
                    .HasComment("Telegram user ID");

                entity.Property(e => e.Views)
                    .HasColumnType("bigint(20)")
                    .HasComment("Number of times the file has been requested");
            });

            modelBuilder.Entity<PomPhoto>(entity =>
            {
                entity.ToTable("POM_Photos");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM photo ID");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of associated file");

                entity.Property(e => e.Height)
                    .HasColumnType("int(11)")
                    .HasComment("File height from Telegram");

                entity.Property(e => e.IsThumbnail).HasComment("Specifies if this photo is a thumbnail for another file");

                entity.Property(e => e.Width)
                    .HasColumnType("int(11)")
                    .HasComment("File width from Telegram");
            });

            modelBuilder.Entity<PomSticker>(entity =>
            {
                entity.ToTable("POM_Stickers");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID for the sticker");

                entity.Property(e => e.Emoji)
                    .HasColumnType("mediumtext")
                    .HasComment("Emoji associated for this sticker in Telegram")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_bin");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of the associated file");

                entity.Property(e => e.Height)
                    .HasColumnType("int(11)")
                    .HasComment("File height from Telegram");

                entity.Property(e => e.IsAnimated).HasComment("Is the sticker animated in Telegram");

                entity.Property(e => e.MaskPoint)
                    .HasColumnType("mediumtext")
                    .HasComment("The part of the face relative to which the mask should be placed in Telegram")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_bin");

                entity.Property(e => e.MaskScale).HasComment("Mask scaling coefficient in Telegram");

                entity.Property(e => e.MaskShiftX).HasComment("Shift by X-axis measured in widths of the mask scaled to the face size in Telegram");

                entity.Property(e => e.MaskShiftY).HasComment("Shift by Y-axis measured in heights of the mask scaled to the face size in Telegram");

                entity.Property(e => e.SetName)
                    .HasColumnType("mediumtext")
                    .HasComment("Name of the source sticker set in Telegram")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_bin");

                entity.Property(e => e.Width)
                    .HasColumnType("int(11)")
                    .HasComment("File width from Telegram");
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

            modelBuilder.Entity<PomVideo>(entity =>
            {
                entity.ToTable("POM_Videos");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM video ID");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasComment("Video length in seconds from Telegram");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of associated file");

                entity.Property(e => e.Height)
                    .HasColumnType("int(11)")
                    .HasComment("File height from Telegram");

                entity.Property(e => e.Width)
                    .HasColumnType("int(11)")
                    .HasComment("File width from Telegram");
            });

            modelBuilder.Entity<PomVideoNote>(entity =>
            {
                entity.ToTable("POM_VideoNotes");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of the video note");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasComment("Duration of video in seconds from Telegram");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of the associated file");

                entity.Property(e => e.Length)
                    .HasColumnType("int(11)")
                    .HasComment("Diameter of video from Telegram");
            });

            modelBuilder.Entity<PomVoice>(entity =>
            {
                entity.ToTable("POM_Voices");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM voice ID");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasComment("Duration in seconds of the voice from Telegram");

                entity.Property(e => e.FileId)
                    .HasColumnType("bigint(20)")
                    .HasComment("POM ID of associated file");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
