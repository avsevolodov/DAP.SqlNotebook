using System;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess
{
    public class SqlNotebookDbContext : DbContext
    {
        public SqlNotebookDbContext(DbContextOptions<SqlNotebookDbContext> options)
            : base(options)
        {
        }

        public DbSet<NotebookEntity> Notebooks => Set<NotebookEntity>();
        public DbSet<NotebookCellEntity> NotebookCells => Set<NotebookCellEntity>();
        public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();

        public DbSet<DbEntityDescription> DbEntities => Set<DbEntityDescription>();
        public DbSet<DbFieldDescription> DbFields => Set<DbFieldDescription>();
        public DbSet<DbRelationDescription> DbRelations => Set<DbRelationDescription>();
        public DbSet<DataMartNodeEntity> DataMartNodes => Set<DataMartNodeEntity>();

        public DbSet<AiAssistMessageEntity> AiAssistMessages => Set<AiAssistMessageEntity>();
        public DbSet<AiAssistSessionEntity> AiAssistSessions => Set<AiAssistSessionEntity>();
        public DbSet<UserEntity> Users => Set<UserEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
                throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.Entity<NotebookEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(512);
                e.HasIndex(x => x.UpdatedAt);
                e.HasIndex(x => x.WorkspaceId);
                e.HasOne(x => x.Workspace)
                    .WithMany()
                    .HasForeignKey(x => x.WorkspaceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<NotebookCellEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Notebook)
                    .WithMany(x => x.Cells)
                    .HasForeignKey(x => x.NotebookId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.NotebookId);
                // SQL Server: nvarchar(n) max is 4000; use nvarchar(max) for large contents
                e.Property(x => x.Content).HasColumnType("nvarchar(max)");
                e.Property(x => x.ExecutionResultJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.DatabaseDisplayName).HasMaxLength(256);
            });

            modelBuilder.Entity<DbEntityDescription>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(512);
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<DbFieldDescription>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.DataType).HasMaxLength(256);
                e.HasIndex(x => new { x.EntityId, x.Name }).IsUnique();
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<DbRelationDescription>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FromFieldName).HasMaxLength(256).IsRequired();
                e.Property(x => x.ToFieldName).HasMaxLength(256).IsRequired();
                e.Property(x => x.Name).HasMaxLength(512);
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<DataMartNodeEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.ParentId);
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
                e.Property(x => x.Owner).HasMaxLength(256);
                e.Property(x => x.Provider).HasMaxLength(64);
                e.Property(x => x.ConnectionInfo).HasMaxLength(2048);
                e.Property(x => x.DatabaseName).HasMaxLength(256);
                e.Property(x => x.AuthType).HasMaxLength(32);
                e.Property(x => x.Login).HasMaxLength(256);
                e.Property(x => x.PasswordEncrypted).HasMaxLength(2048);
                e.Property(x => x.ConsumerGroupPrefix).HasMaxLength(256);
                e.HasOne(x => x.Parent)
                    .WithMany()
                    .HasForeignKey(x => x.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Entity)
                    .WithMany()
                    .HasForeignKey(x => x.EntityId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<WorkspaceEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.Description).HasMaxLength(2048);
                e.Property(x => x.OwnerLogin).HasMaxLength(256);
                e.HasIndex(x => x.OwnerLogin);
            });

            modelBuilder.Entity<AiAssistMessageEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.NotebookId);
                e.HasIndex(x => x.UserLogin);
                e.HasIndex(x => x.SessionId);
                e.Property(x => x.Content).HasColumnType("nvarchar(max)");
                e.Property(x => x.CreatedAt);
                e.Property(x => x.UserLogin).HasMaxLength(256);
                e.HasIndex(x => new { x.UserLogin, x.CreatedAt });
            });

            modelBuilder.Entity<AiAssistSessionEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.UserLogin).HasMaxLength(256);
                e.Property(x => x.Title).HasMaxLength(512);
                e.HasIndex(x => new { x.UserLogin, x.CreatedAt });
            });

            modelBuilder.Entity<UserEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Login).IsUnique();
                e.Property(x => x.Login).HasMaxLength(256);
                e.Property(x => x.Role).HasMaxLength(32);
            });
        }
    }
}
