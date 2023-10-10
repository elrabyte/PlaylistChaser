CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RoleId] int NOT NULL,
    [ClaimType] NVARCHAR(MAX),
    [ClaimValue] NVARCHAR(MAX),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
);
go
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId]);
go