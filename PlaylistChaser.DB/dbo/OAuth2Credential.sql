CREATE TABLE [dbo].[OAuth2Credential]
(
    [UserId] INT NOT NULL, 
    [Provider] NVARCHAR(255) NOT NULL, 
    [AccessToken] NVARCHAR(4000) NOT NULL, 
    [RefreshToken] NVARCHAR(4000) NOT NULL, 
    [TokenExpiration] DATETIME NOT NULL, 
    CONSTRAINT [PK_OAuth2Credential] PRIMARY KEY ([UserId],[Provider])
)

