using Concerto.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.DatabaseContext;

public class AppDataContext : DbContext
{
	public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

	public DbSet<User> Users { get; set; }
	public DbSet<Post> ChatMessages { get; set; }
	public DbSet<Workspace> Workspaces { get; set; }
	public DbSet<WorkspaceUser> WorkspaceUsers { get; set; }
	public DbSet<Session> Sessions { get; set; }
	public DbSet<UploadedFile> UploadedFiles { get; set; }
	public DbSet<Folder> Folders { get; set; }
	public DbSet<UserFolderPermission> UserFolderPermissions { get; set; }
	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }

	public DbSet<DawProject> DawProjects { get; set; }
	public DbSet<Track> Tracks { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Ignore<Entity>();

		// Workspace entity configuration
		modelBuilder.Entity<Workspace>()
			.HasOne(c => c.RootFolder)
			.WithMany()
			.IsRequired(false)
			.HasForeignKey(c => c.RootFolderId);

		modelBuilder.Entity<Workspace>()
			.HasOne(c => c.SessionsFolder)
			.WithMany()
			.IsRequired(false)
			.HasForeignKey(c => c.SessionsFolderId);

		modelBuilder.Entity<Workspace>()
			.HasMany(c => c.Posts)
			.WithOne(c => c.Workspace)
			.HasForeignKey(c => c.WorkspaceId)
			.IsRequired()
			.OnDelete(DeleteBehavior.Cascade);

		// WorkspaceUser entity configuration
		modelBuilder.Entity<WorkspaceUser>()
			.HasKey(cu => new { cu.WorkspaceId, cu.UserId });

		modelBuilder
			.Entity<WorkspaceUser>()
			.HasOne(cu => cu.User)
			.WithMany()
			.HasForeignKey(cu => cu.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder
			.Entity<WorkspaceUser>()
			.HasOne(cu => cu.Workspace)
			.WithMany(c => c.WorkspaceUsers)
			.HasForeignKey(cu => cu.WorkspaceId)
			.OnDelete(DeleteBehavior.Cascade);

		// Session entity configuration
		modelBuilder.Entity<Session>()
			.Property(p => p.MeetingGuid)
			.HasDefaultValueSql("gen_random_uuid()");

		modelBuilder.Entity<Session>()
			.HasOne(s => s.Folder)
			.WithOne()
			.OnDelete(DeleteBehavior.SetNull);

		// Folder entity configuration
		// Folder n-1 Owner
		modelBuilder.Entity<Folder>()
			.HasOne(f => f.Owner)
			.WithMany()
			.IsRequired(false)
			.HasForeignKey(c => c.OwnerId)
			.OnDelete(DeleteBehavior.SetNull);

		// Folder n-1 Workspace
		modelBuilder.Entity<Folder>()
			.HasOne(f => f.Workspace)
			.WithMany()
			.IsRequired()
			.HasForeignKey(f => f.WorkspaceId)
			.OnDelete(DeleteBehavior.Cascade);

		// Folder n-1 Folders
		modelBuilder.Entity<Folder>()
			.HasOne(f => f.Parent)
			.WithMany(p => p.SubFolders)
			.HasForeignKey(f => f.ParentId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Folder>()
			.HasMany(f => f.Files)
			.WithOne(uf => uf.Folder);

		// Folder 1-n FolderUserPermission
		modelBuilder.Entity<Folder>()
			.HasMany(c => c.UserPermissions)
			.WithOne(up => up.Folder);

		// UploadedFile entity configuration

		// UploadedFile n-1 Folder
		modelBuilder.Entity<UploadedFile>()
			.HasOne(uf => uf.Folder)
			.WithMany(f => f.Files)
			.HasForeignKey(uf => uf.FolderId)
			.OnDelete(DeleteBehavior.Cascade);

		// UserFolderPermission entity configuration
		// Key
		modelBuilder.Entity<UserFolderPermission>()
			.HasKey(ufp => new { ufp.UserId, ufp.FolderId });

		// UserFolderPermission n-1 User
		modelBuilder
			.Entity<UserFolderPermission>()
			.HasOne(ufp => ufp.User)
			.WithMany()
			.HasForeignKey(ufp => ufp.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// UserFolderPermission n-1 Folder
		modelBuilder
			.Entity<UserFolderPermission>()
			.HasOne(ufp => ufp.Folder)
			.WithMany(f => f.UserPermissions)
			.HasForeignKey(ufp => ufp.FolderId)
			.OnDelete(DeleteBehavior.Cascade);


		modelBuilder.Entity<Post>()
			.HasMany(uf => uf.ReferencedFiles)
			.WithMany();

		modelBuilder.Entity<Track>()
			.Property(t => t.Id)
			.ValueGeneratedOnAdd();

		modelBuilder.Entity<Track>()
			.HasKey(t => new { t.ProjectId, t.Id });

		modelBuilder.Entity<Track>()
			.HasOne(t => t.Project)
            .WithMany(p => p.Tracks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

		base.OnModelCreating(modelBuilder);
	}
}

