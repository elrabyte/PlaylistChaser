CREATE TABLE [dbo].[SongState]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SongId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [StateId] INT NOT NULL, 
    [LastChecked] DATETIME NOT NULL, 
    CONSTRAINT [FK_SongState_Song] FOREIGN KEY ([SongId]) REFERENCES [Song]([Id]) on delete cascade, 
    CONSTRAINT [FK_SongState_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id]), 
    CONSTRAINT [FK_SongState_State] FOREIGN KEY ([StateId]) REFERENCES [State]([Id])
)
