CREATE TABLE [dbo].[SongAdditionalInfo]
(
    [SongId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [SongIdSource] NVARCHAR(255) NULL, 
    [Name] NVARCHAR(255) NULL, 
    [ArtistName] NVARCHAR(255) NULL, 
    [Url] NVARCHAR(400) NULL, 
    [StateId] INT NOT NULL, 
    [LastChecked] DATETIME NOT NULL, 
    CONSTRAINT [FK_SongAdditionalInfo_Song] FOREIGN KEY ([SongId]) REFERENCES [Song]([Id]), 
    CONSTRAINT [FK_SongAdditionalInfo_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id]), 
    CONSTRAINT [PK_SongAdditionalInfo] PRIMARY KEY ([SongId], [SourceId]),
    CONSTRAINT [FK_SongAdditionalInfo_State] FOREIGN KEY ([StateId]) REFERENCES [State]([Id]),
)
