using Concerto.Server.Data.Models;
using Concerto.Server.Data.Models.Storage;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.DatabaseContext;

public class AppDataContext : DbContext
{
	public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

	public DbSet<User> Users { get; set; }
	public DbSet<Post> ChatMessages { get; set; }
	public DbSet<Course> Courses { get; set; }
	public DbSet<CourseUser> CourseUsers { get; set; }
	public DbSet<Session> Sessions { get; set; }
	public DbSet<UploadedFile> UploadedFiles { get; set; }
	public DbSet<Folder> Folders { get; set; }
	public DbSet<UserFolderPermission> UserFolderPermissions { get; set; }
	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Ignore<Entity>();

		// Course entity configuration
		modelBuilder.Entity<Course>()
			.HasOne(c => c.RootFolder)
			.WithMany()
			.IsRequired(false)
			.HasForeignKey(c => c.RootFolderId);

		modelBuilder.Entity<Course>()
			.HasOne(c => c.SessionsFolder)
			.WithMany()
			.IsRequired(false)
			.HasForeignKey(c => c.SessionsFolderId);

		modelBuilder.Entity<Course>()
			.HasMany(c => c.Posts)
			.WithOne(c => c.Course)
			.HasForeignKey(c => c.CourseId)
			.IsRequired()
			.OnDelete(DeleteBehavior.Cascade);

		// CourseUser entity configuration
		modelBuilder.Entity<CourseUser>()
			.HasKey(cu => new { cu.CourseId, cu.UserId });

		modelBuilder
			.Entity<CourseUser>()
			.HasOne(cu => cu.User)
			.WithMany(c => c.CoursesUser)
			.HasForeignKey(cu => cu.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder
			.Entity<CourseUser>()
			.HasOne(cu => cu.Course)
			.WithMany(c => c.CourseUsers)
			.HasForeignKey(cu => cu.CourseId)
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
			.IsRequired()
			.HasForeignKey(c => c.OwnerId)
			.OnDelete(DeleteBehavior.SetNull);

		// Folder n-1 Course
		modelBuilder.Entity<Folder>()
			.HasOne(f => f.Course)
			.WithMany()
			.IsRequired()
			.HasForeignKey(f => f.CourseId)
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
		

		/*        // Data seed
		        modelBuilder.Entity<User>()
		            .HasData(
		                new User { Id = 1, FirstName = "Jan", LastName = "Administracyjny", Username = "admin", SubjectId = Guid.Parse("95f418ac-e38f-41ec-a2ad-828bdd3895d0") },
		                new User { Id = 2, FirstName = "Piotr", LastName = "Testowy", Username = "user2", SubjectId = Guid.Parse("954af482-22dd-483f-ac99-975144f85a04") },
		                new User { Id = 3, FirstName = "Jacek", LastName = "Testowy", Username = "user3", SubjectId = Guid.Parse("c786cbc3-9924-410f-bcdb-75a2469107be") },
		                new User { Id = 4, FirstName = "John", LastName = "Smith", Username = "user4", SubjectId = Guid.Parse("f2c0a648-82bb-44a9-908e-8006577cb276") }
		            );

		        modelBuilder.Entity<Contact>()
		            .HasData(
		                new Contact { User1Id = 1, User2Id = 2, Status = ContactStatus.Accepted },
		                new Contact { User1Id = 1, User2Id = 3, Status = ContactStatus.Accepted },
		                new Contact { User1Id = 1, User2Id = 4, Status = ContactStatus.Accepted },
		                new Contact { User1Id = 2, User2Id = 3, Status = ContactStatus.Accepted },
		                new Contact { User1Id = 2, User2Id = 4, Status = ContactStatus.Accepted },
		                new Contact { User1Id = 3, User2Id = 4, Status = ContactStatus.Accepted }
		            );


		        modelBuilder.Entity<ConversationUser>()
		            .HasData(
		                new ConversationUser { ConversationId = 1, UserId = 1 },
		                new ConversationUser { ConversationId = 1, UserId = 2 },
		                new ConversationUser { ConversationId = 2, UserId = 1 },
		                new ConversationUser { ConversationId = 2, UserId = 3 },
		                new ConversationUser { ConversationId = 3, UserId = 1 },
		                new ConversationUser { ConversationId = 3, UserId = 4 },
		                new ConversationUser { ConversationId = 4, UserId = 2 },
		                new ConversationUser { ConversationId = 4, UserId = 3 },
		                new ConversationUser { ConversationId = 5, UserId = 2 },
		                new ConversationUser { ConversationId = 5, UserId = 4 },
		                new ConversationUser { ConversationId = 6, UserId = 3 },
		                new ConversationUser { ConversationId = 6, UserId = 4 },

		                // Course 1
		                new ConversationUser { ConversationId = 7, UserId = 1 },
		                new ConversationUser { ConversationId = 7, UserId = 2 },
		                new ConversationUser { ConversationId = 7, UserId = 3 },

		                // Course 2
		                new ConversationUser { ConversationId = 8, UserId = 1 },
		                new ConversationUser { ConversationId = 8, UserId = 4 }
		            );

		        modelBuilder
		            .Entity<Course>()
		            .HasData(
		                new Course { Id = 1, OwnerId = 1, Name = "Course 1", ConversationId = 7 },
		                new Course { Id = 2, OwnerId = 1, Name = "Course 2", ConversationId = 8 }
		            );

		        modelBuilder
		             .Entity<CourseUser>()
		             .HasData(
		                 // Course 1
		                 new CourseUser { CourseId = 1, UserId = 1 },
		                 new CourseUser { CourseId = 1, UserId = 2 },
		                 new CourseUser { CourseId = 1, UserId = 3 },

		                 // Course 2
		                 new CourseUser { CourseId = 2, UserId = 1 },
		                 new CourseUser { CourseId = 2, UserId = 4 }
		             );

		        modelBuilder
		            .Entity<Conversation>()
		            .HasData(
		                new Conversation { Id = 1, IsPrivate = true },
		                new Conversation { Id = 2, IsPrivate = true },
		                new Conversation { Id = 3, IsPrivate = true },
		                new Conversation { Id = 4, IsPrivate = true },
		                new Conversation { Id = 5, IsPrivate = true },
		                new Conversation { Id = 6, IsPrivate = true },
		                new Conversation { Id = 7, IsPrivate = false },
		                new Conversation { Id = 8, IsPrivate = false }
		            );


		        modelBuilder
		            .Entity<ChatMessage>()
		            .HasData(
		                new ChatMessage { Id = 1, SenderId = 1, ConversationId = 1, Content = "Test message 1", SendTimestamp = DateTime.UtcNow.AddMinutes(-5) },
		                new ChatMessage { Id = 2, SenderId = 1, ConversationId = 1, Content = "Test message 2", SendTimestamp = DateTime.UtcNow.AddMinutes(-3) },
		                new ChatMessage { Id = 3, SenderId = 2, ConversationId = 1, Content = "Test reply 1", SendTimestamp = DateTime.UtcNow.AddMinutes(-2) },
		                new ChatMessage { Id = 4, SenderId = 2, ConversationId = 1, Content = "Test reply 2", SendTimestamp = DateTime.UtcNow.AddMinutes(-1) },
		                new ChatMessage { Id = 5, SenderId = 1, ConversationId = 1, Content = "Test message 3", SendTimestamp = DateTime.UtcNow.AddMinutes(-1) }
		            );*/

		base.OnModelCreating(modelBuilder);
	}
}

