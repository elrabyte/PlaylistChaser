CREATE TABLE [dbo].[SongAdditionalInfo]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SongId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [SongIdSource] NVARCHAR(255) NOT NULL, 
    [Name] NVARCHAR(255) NOT NULL, 
    [ArtistName] NVARCHAR(255) NOT NULL, 
    [Url] NVARCHAR(400) NULL, 
    CONSTRAINT [FK_SongAdditionalInfo_Song] FOREIGN KEY ([SongId]) REFERENCES [Song]([Id]), 
    CONSTRAINT [FK_SongAdditionalInfo_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id])
)
