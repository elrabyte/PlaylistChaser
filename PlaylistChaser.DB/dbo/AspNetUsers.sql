CREATE TABLE [dbo].[AspNetUsers] (
    [Id] int NOT NULL PRIMARY KEY IDENTITY(10, 1),
    [UserName] NVARCHAR(256),
    [NormalizedUserName] NVARCHAR(256),
    [Email] NVARCHAR(256),
    [NormalizedEmail] NVARCHAR(256),
    [EmailConfirmed] BIT NOT NULL,
    [PasswordHash] NVARCHAR(MAX),
    [SecurityStamp] NVARCHAR(MAX),
    [ConcurrencyStamp] NVARCHAR(MAX),
    [PhoneNumber] NVARCHAR(MAX),
    [PhoneNumberConfirmed] BIT NOT NULL,
    [TwoFactorEnabled] BIT NOT NULL,
    [LockoutEnd] DateTimeOffset,
    [LockoutEnabled] BIT NOT NULL,
    [AccessFailedCount] INT NOT NULL, 
    [DbUserName] NVARCHAR(255) NULL, 
    [DbPassword] NVARCHAR(255) NULL
);
go

CREATE UNIQUE INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL;
go
CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
go