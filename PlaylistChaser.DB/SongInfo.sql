CREATE TABLE [dbo].[SongInfo]
(
    [SongId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [SongIdSource] NVARCHAR(255) NOT NULL, 
    [Name] NVARCHAR(255) NOT NULL, 
    [ArtistName] NVARCHAR(255) NOT NULL, 
    [Url] NVARCHAR(400) NOT NULL, 
    CONSTRAINT [PK_SongAdditionalInfo] PRIMARY KEY ([SongId], [SourceId]),
    CONSTRAINT [FK_SongAdditionalInfo_Song] FOREIGN KEY ([SongId]) REFERENCES [Song]([Id]), 
    CONSTRAINT [FK_SongAdditionalInfo_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id]), 
    CONSTRAINT [AK_SongAdditionalInfo_PlaylistIdSource] UNIQUE([SourceId], [SongIdSource]),
)
