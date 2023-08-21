CREATE TABLE [dbo].[PlaylistSongState]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [PlaylistSongId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [StateId] INT NOT NULL, 
    [LastChecked] DATETIME NOT NULL, 
    CONSTRAINT [FK_PlaylistSongState_PlaylistSong] FOREIGN KEY ([PlaylistSongId]) REFERENCES [PlaylistSong]([Id]) on delete cascade, 
    CONSTRAINT [FK_PlaylistSongState_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id]), 
    CONSTRAINT [FK_PlaylistSongState_State] FOREIGN KEY ([StateId]) REFERENCES [State]([Id])
)
