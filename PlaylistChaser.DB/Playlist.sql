CREATE TABLE [dbo].[Playlist]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(MAX) NOT NULL, 
    [Description] NVARCHAR(MAX) NULL, 
    [ChannelName] NVARCHAR(255) NOT NULL, 
    [PlaylistTypeId] INT NOT NULL, 
    [ThumbnailId] INT NULL, 
    CONSTRAINT [FK_Playlist_PlaylistType] FOREIGN KEY ([PlaylistTypeId]) REFERENCES [PlaylistType]([Id]), 
    CONSTRAINT [FK_Playlist_Thumbnail] FOREIGN KEY ([ThumbnailId]) REFERENCES [Thumbnail]([Id])
)

