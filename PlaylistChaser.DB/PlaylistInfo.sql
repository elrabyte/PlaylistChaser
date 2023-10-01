CREATE TABLE [dbo].[PlaylistInfo]
(
    [PlaylistId] INT NOT NULL, 
    [SourceId] INT NOT NULL, 
    [PlaylistIdSource] NVARCHAR(255) NOT NULL, 
    [Name] NVARCHAR(255) NOT NULL, 
    [CreatorName] NVARCHAR(255) NOT NULL, 
    [IsMine] BIT NOT NULL, 
    [Description] NVARCHAR(MAX) NULL, 
    [Url] NVARCHAR(400) NOT NULL, 
    [LastSynced] DATETIME NOT NULL, 
    CONSTRAINT [PK_PlaylistAdditionalInfo] PRIMARY KEY ([PlaylistId], [SourceId]),
    CONSTRAINT [FK_PlaylistAdditionalInfo_Playlist] FOREIGN KEY ([PlaylistId]) REFERENCES [Playlist]([Id]), 
    CONSTRAINT [FK_PlaylistAdditionalInfo_Source] FOREIGN KEY ([SourceId]) REFERENCES [Source]([Id]), 
    CONSTRAINT [AK_PlaylistAdditionalInfo_PlaylistIdSource] UNIQUE([SourceId], [PlaylistIdSource]),
)
