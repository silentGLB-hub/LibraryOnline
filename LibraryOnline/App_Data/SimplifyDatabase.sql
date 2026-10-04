-- Convert the old Identity tables to one Users table. Back up the database first.
-- Run in LibraryOnlineDemo. Existing user IDs, passwords and loan links are preserved.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.AspNetUsers') IS NOT NULL
BEGIN
 IF EXISTS(SELECT 1 FROM dbo.AspNetUserClaims) OR EXISTS(SELECT 1 FROM dbo.AspNetUserLogins)
  THROW 50001, 'External logins or claims exist. Migration stopped to preserve them.', 1;
 IF EXISTS(SELECT u.Id FROM dbo.AspNetUsers u LEFT JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id GROUP BY u.Id HAVING COUNT(ur.RoleId)<>1)
  THROW 50002, 'Each user must have exactly one role before migration.', 1;
 IF EXISTS(SELECT 1 FROM dbo.AspNetRoles WHERE Name NOT IN ('Member','Librarian','Administrator'))
  THROW 50003, 'Unknown roles exist. Migration stopped.', 1;
 ALTER TABLE dbo.AspNetUsers ADD Role nvarchar(20) NULL;
 EXEC(N'UPDATE u SET Role=r.Name FROM dbo.AspNetUsers u JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId');
 ALTER TABLE dbo.AspNetUsers ALTER COLUMN Role nvarchar(20) NOT NULL;
 DROP TABLE dbo.AspNetUserClaims;
 DROP TABLE dbo.AspNetUserLogins;
 DROP TABLE dbo.AspNetUserRoles;
 DROP TABLE dbo.AspNetRoles;
 DROP INDEX UserNameIndex ON dbo.AspNetUsers;
 ALTER TABLE dbo.AspNetUsers DROP COLUMN UserName,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled;
 EXEC sp_rename 'dbo.AspNetUsers','Users';

 ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Role CHECK(Role IN ('Member','Librarian','Administrator'));
 IF OBJECT_ID('dbo.__MigrationHistory') IS NOT NULL DROP TABLE dbo.__MigrationHistory;
END;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Users') AND name='IX_Email') DROP INDEX IX_Email ON dbo.Users;
ALTER TABLE dbo.Users ALTER COLUMN Email nvarchar(256) NOT NULL;
CREATE UNIQUE INDEX IX_Email ON dbo.Users(Email);
COMMIT;
