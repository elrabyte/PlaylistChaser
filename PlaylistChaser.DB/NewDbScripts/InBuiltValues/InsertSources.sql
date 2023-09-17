CREATE PROCEDURE [dbo].[InsertSources]
AS
set identity_insert [dbo].[Source] on;

insert into [dbo].[Source] (id, name, IconHtml) 
	 values (1, 'Youtube', '<i class=''bi bi-youtube''></i>'),
			(2, 'Spotify', '<i class=''bi bi-spotify''></i>')

set identity_insert [dbo].[Source] off;