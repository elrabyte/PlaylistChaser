﻿CREATE TABLE [dbo].[Playlists]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(MAX) NOT NULL, 
    [Description] NVARCHAR(MAX) NULL, 
    [YoutubeUrl] NVARCHAR(255) NOT NULL, 
    [YoutubeId] NVARCHAR(255) NOT NULL, 
    [ChannelName] NVARCHAR(255) NOT NULL, 
    [ImageBytes64] NVARCHAR(MAX) NULL, 
    [SpotifyUrl] NVARCHAR(255) NULL, 
    [PlaylistTypeId] INT NOT NULL, 
    CONSTRAINT [FK_Playlists_PlaylistTypes] FOREIGN KEY ([PlaylistTypeId]) REFERENCES [PlaylistTypes]([Id])
)

