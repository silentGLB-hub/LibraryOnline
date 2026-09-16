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
