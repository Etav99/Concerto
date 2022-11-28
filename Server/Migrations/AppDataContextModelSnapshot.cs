﻿// <auto-generated />
using System;
using Concerto.Server.Data.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concerto.Server.Migrations
{
    [DbContext(typeof(AppDataContext))]
    partial class AppDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Concerto.Server.Data.Models.ChatMessage", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("ConversationId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("SendTimestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("SenderId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("SenderId");

                    b.ToTable("ChatMessages");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Conversation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("Conversations");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.ConversationUser", b =>
                {
                    b.Property<long>("ConversationId")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("ConversationId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("ConversationUsers");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ConversationId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("OwnerId")
                        .HasColumnType("bigint");

                    b.Property<long?>("RootFolderId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("RootFolderId");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.CourseUser", b =>
                {
                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.HasKey("CourseId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("CourseUsers");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Folder", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("OwnerId")
                        .HasColumnType("bigint");

                    b.Property<long?>("ParentId")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.HasIndex("OwnerId");

                    b.HasIndex("ParentId");

                    b.ToTable("Folders");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Session", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ConversationId")
                        .HasColumnType("bigint");

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("MeetingGuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("ScheduledDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("CourseId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.UploadedFile", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("FolderId")
                        .HasColumnType("bigint");

                    b.Property<long>("OwnerId")
                        .HasColumnType("bigint");

                    b.Property<string>("StorageName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("FolderId");

                    b.ToTable("UploadedFiles");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("SubjectId")
                        .HasColumnType("uuid");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("SubjectId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.UserFolderPermission", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<long>("FolderId")
                        .HasColumnType("bigint");

                    b.HasKey("UserId", "FolderId");

                    b.HasIndex("FolderId");

                    b.ToTable("UserFolderPermissions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.ChatMessage", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Conversation", "Conversation")
                        .WithMany("ChatMessages")
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Conversation");

                    b.Navigation("Sender");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.ConversationUser", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Conversation", "Conversation")
                        .WithMany("ConversationUsers")
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "User")
                        .WithMany("ConversationsUser")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Conversation");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Conversation", "Conversation")
                        .WithMany()
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Folder", "RootFolder")
                        .WithMany()
                        .HasForeignKey("RootFolderId");

                    b.Navigation("Conversation");

                    b.Navigation("RootFolder");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.CourseUser", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany("CourseUsers")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "User")
                        .WithMany("CoursesUser")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Course");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Folder", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Folder", "Parent")
                        .WithMany("SubFolders")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.OwnsOne("Concerto.Server.Data.Models.FolderPermission", "CoursePermission", b1 =>
                        {
                            b1.Property<long>("FolderId")
                                .HasColumnType("bigint");

                            b1.Property<bool>("Inherited")
                                .HasColumnType("boolean");

                            b1.Property<int>("Type")
                                .HasColumnType("integer");

                            b1.HasKey("FolderId");

                            b1.ToTable("Folders");

                            b1.WithOwner()
                                .HasForeignKey("FolderId");
                        });

                    b.Navigation("Course");

                    b.Navigation("CoursePermission")
                        .IsRequired();

                    b.Navigation("Owner");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Session", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Conversation", "Conversation")
                        .WithMany()
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany("Sessions")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Conversation");

                    b.Navigation("Course");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.UploadedFile", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Folder", "Folder")
                        .WithMany("Files")
                        .HasForeignKey("FolderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Folder");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.UserFolderPermission", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Folder", "Folder")
                        .WithMany("UserPermissions")
                        .HasForeignKey("FolderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Concerto.Server.Data.Models.FolderPermission", "Permission", b1 =>
                        {
                            b1.Property<long>("UserFolderPermissionUserId")
                                .HasColumnType("bigint");

                            b1.Property<long>("UserFolderPermissionFolderId")
                                .HasColumnType("bigint");

                            b1.Property<bool>("Inherited")
                                .HasColumnType("boolean");

                            b1.Property<int>("Type")
                                .HasColumnType("integer");

                            b1.HasKey("UserFolderPermissionUserId", "UserFolderPermissionFolderId");

                            b1.ToTable("UserFolderPermissions");

                            b1.WithOwner()
                                .HasForeignKey("UserFolderPermissionUserId", "UserFolderPermissionFolderId");
                        });

                    b.Navigation("Folder");

                    b.Navigation("Permission")
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Conversation", b =>
                {
                    b.Navigation("ChatMessages");

                    b.Navigation("ConversationUsers");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.Navigation("CourseUsers");

                    b.Navigation("Sessions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Folder", b =>
                {
                    b.Navigation("Files");

                    b.Navigation("SubFolders");

                    b.Navigation("UserPermissions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.User", b =>
                {
                    b.Navigation("ConversationsUser");

                    b.Navigation("CoursesUser");
                });
#pragma warning restore 612, 618
        }
    }
}
