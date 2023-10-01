CREATE TABLE [dbo].[Song]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ArtistName] NVARCHAR(MAX) NOT NULL, 
    [SongName] NVARCHAR(MAX) NOT NULL, 
    [ThumbnailId] INT NULL, 
    CONSTRAINT [FK_Song_Thumbnail] FOREIGN KEY ([ThumbnailId]) REFERENCES [Thumbnail]([Id])
)
