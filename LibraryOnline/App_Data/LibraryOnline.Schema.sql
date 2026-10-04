-- SQL Server schema generated from the application's EF6 model.
-- Execute in an EMPTY database selected in SSMS.
-- Demo data is created only when the application creates a new database itself.
create table [dbo].[AspNetUsers] (
    [Id] [nvarchar](128) not null,
    [FullName] [nvarchar](100) not null,
    [IsActive] [bit] not null,
    [JoinedAt] [datetime] not null,
    [Email] [nvarchar](256) null,
    [EmailConfirmed] [bit] not null,
    [PasswordHash] [nvarchar](max) null,
    [SecurityStamp] [nvarchar](max) null,
    [PhoneNumber] [nvarchar](max) null,
    [PhoneNumberConfirmed] [bit] not null,
    [TwoFactorEnabled] [bit] not null,
    [LockoutEndDateUtc] [datetime] null,
    [LockoutEnabled] [bit] not null,
    [AccessFailedCount] [int] not null,
    [UserName] [nvarchar](256) not null,
    primary key ([Id])
);
create table [dbo].[Books] (
    [Id] [int] not null identity,
    [Title] [nvarchar](200) not null,
    [Author] [nvarchar](150) not null,
    [Publisher] [nvarchar](150) not null,
    [PublicationYear] [int] not null,
    [ISBN] [nvarchar](20) not null,
    [Description] [nvarchar](max) not null,
    [CoverImage] [nvarchar](1000) null,
    [CategoryId] [int] not null,
    [TotalCopies] [int] not null,
    [AvailableCopies] [int] not null,
    [IsActive] [bit] not null,
    [RowVersion] [rowversion] not null,
    primary key ([Id])
);
create table [dbo].[Categories] (
    [Id] [int] not null identity,
    [Name] [nvarchar](80) not null,
    primary key ([Id])
);
create table [dbo].[Fines] (
    [Id] [int] not null identity,
    [LoanId] [int] not null,
    [Amount] [decimal](18, 2) not null,
    [Paid] [decimal](18, 2) not null,
    primary key ([Id])
);
create table [dbo].[FinePayments] (
    [Id] [int] not null identity,
    [FineId] [int] not null,
    [Amount] [decimal](18, 2) not null,
    [PaidAt] [datetime] not null,
    [RecordedBy] [nvarchar](max) null,
    [Note] [nvarchar](200) null,
    primary key ([Id])
);
create table [dbo].[GenrePreferences] (
    [Id] [int] not null identity,
    [MemberId] [nvarchar](128) not null,
    [CategoryId] [int] not null,
    primary key ([Id])
);
create table [dbo].[AspNetRoles] (
    [Id] [nvarchar](128) not null,
    [Name] [nvarchar](256) not null,
    primary key ([Id])
);
create table [dbo].[AspNetUserClaims] (
    [Id] [int] not null identity,
    [UserId] [nvarchar](128) not null,
    [ClaimType] [nvarchar](max) null,
    [ClaimValue] [nvarchar](max) null,
    primary key ([Id])
);
create table [dbo].[AspNetUserLogins] (
    [LoginProvider] [nvarchar](128) not null,
    [ProviderKey] [nvarchar](128) not null,
    [UserId] [nvarchar](128) not null,
    primary key ([LoginProvider], [ProviderKey], [UserId])
);
create table [dbo].[AspNetUserRoles] (
    [UserId] [nvarchar](128) not null,
    [RoleId] [nvarchar](128) not null,
    primary key ([UserId], [RoleId])
);
create table [dbo].[LibraryPolicies] (
    [Id] [int] not null identity,
    [MaxActiveLoans] [int] not null,
    [LoanDays] [int] not null,
    [PickupDays] [int] not null,
    [RenewalDays] [int] not null,
    [MaxRenewals] [int] not null,
    [FinePerDay] [decimal](18, 2) not null,
    [LibraryName] [nvarchar](100) not null,
    primary key ([Id])
);
create table [dbo].[Loans] (
    [Id] [int] not null identity,
    [MemberId] [nvarchar](128) not null,
    [BookId] [int] not null,
    [Status] [int] not null,
    [ReservedAt] [datetime] not null,
    [PickupExpiresAt] [datetime] not null,
    [BorrowedAt] [datetime] null,
    [DueAt] [datetime] null,
    [ReturnedAt] [datetime] null,
    [DailyFineRate] [decimal](18, 2) not null,
    [RenewalCount] [int] not null,
    primary key ([Id])
);
create table [dbo].[Notifications] (
    [Id] [int] not null identity,
    [MemberId] [nvarchar](max) not null,
    [EventKey] [nvarchar](200) not null,
    [Message] [nvarchar](500) not null,
    [CreatedAt] [datetime] not null,
    [IsRead] [bit] not null,
    primary key ([Id])
);
create table [dbo].[ReadingLists] (
    [Id] [int] not null identity,
    [MemberId] [nvarchar](max) not null,
    [Name] [nvarchar](80) not null,
    primary key ([Id])
);
create table [dbo].[ReadingListItems] (
    [Id] [int] not null identity,
    [ReadingListId] [int] not null,
    [BookId] [int] not null,
    primary key ([Id])
);
create table [dbo].[RenewalRequests] (
    [Id] [int] not null identity,
    [LoanId] [int] not null,
    [RequestedAt] [datetime] not null,
    [Status] [int] not null,
    [Note] [nvarchar](400) null,
    [DecidedAt] [datetime] null,
    primary key ([Id])
);
alter table [dbo].[AspNetUserClaims] add constraint [AppUser_Claims] foreign key ([UserId]) references [dbo].[AspNetUsers]([Id]);
alter table [dbo].[AspNetUserLogins] add constraint [AppUser_Logins] foreign key ([UserId]) references [dbo].[AspNetUsers]([Id]);
alter table [dbo].[AspNetUserRoles] add constraint [AppUser_Roles] foreign key ([UserId]) references [dbo].[AspNetUsers]([Id]);
alter table [dbo].[Books] add constraint [Book_Category] foreign key ([CategoryId]) references [dbo].[Categories]([Id]);
alter table [dbo].[Fines] add constraint [Fine_Loan] foreign key ([LoanId]) references [dbo].[Loans]([Id]);
alter table [dbo].[FinePayments] add constraint [FinePayment_Fine] foreign key ([FineId]) references [dbo].[Fines]([Id]);
alter table [dbo].[GenrePreferences] add constraint [GenrePreference_Category] foreign key ([CategoryId]) references [dbo].[Categories]([Id]);
alter table [dbo].[AspNetUserRoles] add constraint [IdentityRole_Users] foreign key ([RoleId]) references [dbo].[AspNetRoles]([Id]);
alter table [dbo].[Loans] add constraint [Loan_Book] foreign key ([BookId]) references [dbo].[Books]([Id]);
alter table [dbo].[Loans] add constraint [Loan_Member] foreign key ([MemberId]) references [dbo].[AspNetUsers]([Id]);
alter table [dbo].[ReadingListItems] add constraint [ReadingListItem_Book] foreign key ([BookId]) references [dbo].[Books]([Id]);
alter table [dbo].[ReadingListItems] add constraint [ReadingListItem_ReadingList] foreign key ([ReadingListId]) references [dbo].[ReadingLists]([Id]);
alter table [dbo].[RenewalRequests] add constraint [RenewalRequest_Loan] foreign key ([LoanId]) references [dbo].[Loans]([Id]);

GO
-- Additive migration: does not modify EF6's existing schema or delete data.
IF OBJECT_ID(N'dbo.BookMaterials',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BookMaterials (
  BookId int NOT NULL CONSTRAINT PK_BookMaterials PRIMARY KEY,
  AuthorBiography nvarchar(max) NOT NULL,
  WorkIntroduction nvarchar(max) NOT NULL,
  PublisherInformation nvarchar(max) NOT NULL,
  PreviewText nvarchar(max) NOT NULL,
  FullText nvarchar(max) NOT NULL,
  IsPublished bit NOT NULL,
  IsDemo bit NOT NULL,
  UpdatedAt datetime2 NOT NULL,
  Version rowversion NOT NULL,
  CONSTRAINT FK_BookMaterials_Books FOREIGN KEY(BookId) REFERENCES dbo.Books(Id) ON DELETE CASCADE
 );
END;
