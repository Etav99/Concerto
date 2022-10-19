﻿using Concerto.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.DatabaseContext;

public class AppDataContext : DbContext
{

    public DbSet<User> Users { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomUser> RoomUsers { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<Catalog> Catalogs { get; set; }

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Entity>();

        // Contact entity configuration
        modelBuilder.Entity<Contact>().HasKey(uc => new { uc.User1Id, uc.User2Id });

        modelBuilder.Entity<Contact>()
            .HasOne(c => c.User1)
            .WithMany(u => u.InvitedContacts)
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contact>()
            .HasOne(c => c.User2)
            .WithMany(u => u.InvitingContacts)
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // ConversationUser entity configuration
        modelBuilder.Entity<ConversationUser>()
            .HasKey(cu => new { cu.ConversationId, cu.UserId });

        modelBuilder.Entity<ConversationUser>()
            .HasOne(cu => cu.Conversation)
            .WithMany(c => c.ConversationUsers)
            .HasForeignKey(cu => cu.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConversationUser>()
            .HasOne(cu => cu.User)
            .WithMany(u => u.ConversationsUser)
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // RoomUser entity configuration
        modelBuilder.Entity<RoomUser>()
            .HasKey(ru => new { ru.RoomId, ru.UserId });

        modelBuilder
            .Entity<RoomUser>()
            .HasOne(ru => ru.User)
            .WithMany(u => u.RoomsUser)
            .HasForeignKey(ru => ru.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<RoomUser>()
            .HasOne(ru => ru.Room)
            .WithMany(r => r.RoomUsers)
            .HasForeignKey(ru => ru.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Session entity configuration
        modelBuilder.Entity<Session>()
            .Property(p => p.MeetingGuid)
            .HasDefaultValueSql("gen_random_uuid()");

        // Catalog entity configuration
        modelBuilder.Entity<Catalog>()
            .HasOne(c => c.Owner)
            .WithMany()
            .IsRequired()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Catalog>()
            .HasMany(c => c.UsersSharedTo)
            .WithMany(u => u.CatalogsSharedTo)
            .UsingEntity(j => j.ToTable("CatalogUser"));


        // Data seed
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

                // Room 1
                new ConversationUser { ConversationId = 7, UserId = 1 },
                new ConversationUser { ConversationId = 7, UserId = 2 },
                new ConversationUser { ConversationId = 7, UserId = 3 },

                // Room 2
                new ConversationUser { ConversationId = 8, UserId = 1 },
                new ConversationUser { ConversationId = 8, UserId = 4 }
            );

        modelBuilder
            .Entity<Room>()
            .HasData(
                new Room { Id = 1, OwnerId = 1, Name = "Room 1", ConversationId = 7 },
                new Room { Id = 2, OwnerId = 1, Name = "Room 2", ConversationId = 8 }
            );

        modelBuilder
             .Entity<RoomUser>()
             .HasData(
                 // Room 1
                 new RoomUser { RoomId = 1, UserId = 1 },
                 new RoomUser { RoomId = 1, UserId = 2 },
                 new RoomUser { RoomId = 1, UserId = 3 },

                 // Room 2
                 new RoomUser { RoomId = 2, UserId = 1 },
                 new RoomUser { RoomId = 2, UserId = 4 }
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
            );

        base.OnModelCreating(modelBuilder);
    }
}
