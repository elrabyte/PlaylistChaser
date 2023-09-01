CREATE TABLE [dbo].[CombinedPlaylistEntry]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [CombinedPlaylistId] INT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    CONSTRAINT [FK_CombinedPlaylistEntry_PLaylist_CombinedPlaylist] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id]) on delete cascade,
    CONSTRAINT [FK_CombinedPlaylistEntry_PLaylist] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id])
)
