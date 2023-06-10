﻿// <auto-generated />
using System;
using Concerto.Server.Data.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concerto.Server.Migrations
{
    [DbContext(typeof(AppDataContext))]
    [Migration("20230610180444_AddTrackSelectedByName")]
    partial class AddTrackSelectedByName
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Concerto.Server.Data.Models.Comment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("Edited")
                        .HasColumnType("boolean");

                    b.Property<long>("PostId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("PostId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long?>("RootFolderId")
                        .HasColumnType("bigint");

                    b.Property<long?>("SessionsFolderId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("RootFolderId");

                    b.HasIndex("SessionsFolderId");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.CourseUser", b =>
                {
                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.HasKey("CourseId", "UserId");

                    b.HasIndex("CourseId");

                    b.HasIndex("UserId");

                    b.ToTable("CourseUsers");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.DawProject", b =>
                {
                    b.Property<long>("ProjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ProjectId"));

                    b.Property<Guid?>("AudioSourceGuid")
                        .HasColumnType("uuid");

                    b.Property<string>("AudioSourceHash")
                        .HasColumnType("text");

                    b.HasKey("ProjectId");

                    b.ToTable("DawProjects");
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

                    b.Property<Guid?>("OwnerId")
                        .HasColumnType("uuid");

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

            modelBuilder.Entity("Concerto.Server.Data.Models.Post", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("Edited")
                        .HasColumnType("boolean");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("CourseId");

                    b.ToTable("Post");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Session", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<long>("FolderId")
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

                    b.HasIndex("CourseId");

                    b.HasIndex("FolderId")
                        .IsUnique();

                    b.HasIndex("MeetingGuid");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Track", b =>
                {
                    b.Property<long>("ProjectId")
                        .HasColumnType("bigint");

                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Guid?>("AudioSourceGuid")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid?>("SelectedByUserId")
                        .HasColumnType("uuid");

                    b.Property<float>("StartTime")
                        .HasColumnType("real");

                    b.Property<float>("Volume")
                        .HasColumnType("real");

                    b.HasKey("ProjectId", "Id");

                    b.HasIndex("SelectedByUserId");

                    b.ToTable("Tracks");
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

                    b.Property<string>("Extension")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("FolderId")
                        .HasColumnType("bigint");

                    b.Property<string>("MimeType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("OwnerId")
                        .HasColumnType("uuid");

                    b.Property<long>("Size")
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
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.UserFolderPermission", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("FolderId")
                        .HasColumnType("bigint");

                    b.HasKey("UserId", "FolderId");

                    b.HasIndex("FolderId");

                    b.HasIndex("UserId");

                    b.ToTable("UserFolderPermissions");
                });

            modelBuilder.Entity("PostUploadedFile", b =>
                {
                    b.Property<long>("PostId")
                        .HasColumnType("bigint");

                    b.Property<long>("ReferencedFilesId")
                        .HasColumnType("bigint");

                    b.HasKey("PostId", "ReferencedFilesId");

                    b.HasIndex("ReferencedFilesId");

                    b.ToTable("PostUploadedFile");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Comment", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Post", "Post")
                        .WithMany("Comments")
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");

                    b.Navigation("Post");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Folder", "RootFolder")
                        .WithMany()
                        .HasForeignKey("RootFolderId");

                    b.HasOne("Concerto.Server.Data.Models.Folder", "SessionsFolder")
                        .WithMany()
                        .HasForeignKey("SessionsFolderId");

                    b.Navigation("RootFolder");

                    b.Navigation("SessionsFolder");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.CourseUser", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany("CourseUsers")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "User")
                        .WithMany()
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
                        .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity("Concerto.Server.Data.Models.Post", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany("Posts")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");

                    b.Navigation("Course");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Session", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Course", "Course")
                        .WithMany("Sessions")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.Folder", "Folder")
                        .WithOne()
                        .HasForeignKey("Concerto.Server.Data.Models.Session", "FolderId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("Course");

                    b.Navigation("Folder");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Track", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.DawProject", "Project")
                        .WithMany("Tracks")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.User", "SelectedByUser")
                        .WithMany()
                        .HasForeignKey("SelectedByUserId");

                    b.Navigation("Project");

                    b.Navigation("SelectedByUser");
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
                            b1.Property<Guid>("UserFolderPermissionUserId")
                                .HasColumnType("uuid");

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

            modelBuilder.Entity("PostUploadedFile", b =>
                {
                    b.HasOne("Concerto.Server.Data.Models.Post", null)
                        .WithMany()
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Concerto.Server.Data.Models.UploadedFile", null)
                        .WithMany()
                        .HasForeignKey("ReferencedFilesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Course", b =>
                {
                    b.Navigation("CourseUsers");

                    b.Navigation("Posts");

                    b.Navigation("Sessions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.DawProject", b =>
                {
                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Folder", b =>
                {
                    b.Navigation("Files");

                    b.Navigation("SubFolders");

                    b.Navigation("UserPermissions");
                });

            modelBuilder.Entity("Concerto.Server.Data.Models.Post", b =>
                {
                    b.Navigation("Comments");
                });
#pragma warning restore 612, 618
        }
    }
}
