﻿CREATE TABLE [dbo].[Source]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(50) NOT NULL, 
    [IconHtml] NVARCHAR(255) NULL, 
    [ColorHex] VARCHAR(6) NULL
)
