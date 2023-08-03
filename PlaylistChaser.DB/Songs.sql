﻿CREATE TABLE [dbo].[Songs]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [YoutubeSongName] NVARCHAR(255) NULL, 
    [FoundOnSpotify] BIT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    [ArtistName] NVARCHAR(255) NULL, 
    [YoutubeId] NVARCHAR(255) NOT NULL, 
    [SpotifyId] NVARCHAR(255) NULL, 
    [SongName] NVARCHAR(255) NULL, 
    [ImageBytes64] NVARCHAR(4000) NULL, 
    [AddedToSpotify] BIT NOT NULL, 
    [IsNotOnSpotify] BIT NULL
)