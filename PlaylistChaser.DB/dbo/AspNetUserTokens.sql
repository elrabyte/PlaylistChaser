CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] NVARCHAR(128) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Value] NVARCHAR(MAX),
    PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);