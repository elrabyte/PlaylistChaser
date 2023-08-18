CREATE TABLE [dbo].[CombinedPlaylist]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [CombinedPLaylistId] INT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    CONSTRAINT [FK_CombinedPlaylist_PLaylist_Combined] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id]),
    CONSTRAINT [FK_CombinedPlaylist_PLaylist] FOREIGN KEY ([CombinedPLaylistId]) REFERENCES [PLaylist]([Id])
)
