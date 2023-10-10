CREATE TABLE [dbo].[AspNetRoles] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256),
    [NormalizedName] NVARCHAR(256),
    [ConcurrencyStamp] NVARCHAR(MAX)
);
go

CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
go