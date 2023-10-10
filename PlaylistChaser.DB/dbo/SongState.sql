CREATE TABLE [dbo].[SongState]
(
    [SongId] INT NOT NULL, 
    [SourceId] INT NOT NULL,     
    [StateId] INT NOT NULL, 
    [LastChecked] DATETIME NOT NULL, 
    CONSTRAINT [PK_SongState] PRIMARY KEY ([SongId], [SourceId]), 
    CONSTRAINT [FK_SongState_State] FOREIGN KEY ([StateId]) REFERENCES [State]([Id]),
)
