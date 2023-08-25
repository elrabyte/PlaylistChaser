CREATE TABLE [dbo].[PlaylistAdditionalInfo]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [PlaylistId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [PlaylistIdSource] NVARCHAR(255) NOT NULL, 
    [Name] NVARCHAR(255) NOT NULL, 
    [CreatorName] NVARCHAR(255) NOT NULL, 
    [IsMine] BIT NOT NULL, 
    [Description] NVARCHAR(MAX) NULL, 
    [Url] NVARCHAR(400) NULL, 
    CONSTRAINT [FK_PlaylistAdditionalInfo_Playlist] FOREIGN KEY ([PlaylistId]) REFERENCES [Playlist]([Id]), 
    CONSTRAINT [FK_PlaylistAdditionalInfo_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id])
)
