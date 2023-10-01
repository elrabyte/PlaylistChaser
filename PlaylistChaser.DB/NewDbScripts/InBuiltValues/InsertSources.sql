CREATE PROCEDURE [dbo].[InsertSources]
AS
set identity_insert [dbo].[Source] on;

insert into [dbo].[Source] (id, name, IconHtml, ColorHex) 
	 values (1, 'Youtube', '<i class=''bi bi-youtube''></i>', 'ff0000'),
			(2, 'Spotify', '<i class=''bi bi-spotify''></i>', '1DB954')

set identity_insert [dbo].[Source] off;