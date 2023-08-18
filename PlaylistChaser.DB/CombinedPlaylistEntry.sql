CREATE TABLE [dbo].[CombinedPlaylistEntry]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [CombinedPLaylistId] INT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    CONSTRAINT [FK_CombinedPlaylistEntry_PLaylist_CombinedPlaylist] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id]),
    CONSTRAINT [FK_CombinedPlaylistEntry_PLaylist] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id])
)
