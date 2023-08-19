CREATE TABLE [dbo].[PlaylistSong]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SongId] INT NOT NULL, 
    [PlaylistId] INT NOT NULL, 
    CONSTRAINT [FK_PlaylistSong_Song] FOREIGN KEY ([SongId]) REFERENCES [Song]([Id]), 
    CONSTRAINT [FK_PlaylistSong_Playlist] FOREIGN KEY ([PlaylistId]) REFERENCES [Playlist]([Id]), 
)

