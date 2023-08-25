CREATE TABLE [dbo].[Song]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ArtistName] NVARCHAR(MAX) NULL, 
    [SongName] NVARCHAR(MAX) NULL, 
    [ThumbnailId] INT NULL, 
    CONSTRAINT [FK_Song_Thumbnail] FOREIGN KEY ([ThumbnailId]) REFERENCES [Thumbnail]([Id])
)
