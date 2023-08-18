CREATE TABLE [dbo].[Song]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [YoutubeSongName] NVARCHAR(MAX) NULL, 
    [FoundOnSpotify] BIT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    [ArtistName] NVARCHAR(MAX) NULL, 
    [YoutubeId] NVARCHAR(255) NOT NULL, 
    [SpotifyId] NVARCHAR(255) NULL, 
    [SongName] NVARCHAR(MAX) NULL, 
    [ImageBytes64] NVARCHAR(MAX) NULL, 
    [AddedToSpotify] BIT NOT NULL, 
    [IsNotOnSpotify] BIT NULL, 
    CONSTRAINT [FK_Song_Playlist] FOREIGN KEY ([PlaylistId]) REFERENCES [Playlist]([Id])
)
