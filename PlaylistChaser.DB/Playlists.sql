CREATE TABLE [dbo].[Playlists]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(255) NOT NULL, 
    [Description] NVARCHAR(255) NULL, 
    [YoutubeUrl] NVARCHAR(255) NOT NULL, 
    [ChannelName] NVARCHAR(255) NOT NULL, 
    [ImageBytes64] BINARY(4000) NULL, 
    [SpotifyUrl] NVARCHAR(255) NULL
)

