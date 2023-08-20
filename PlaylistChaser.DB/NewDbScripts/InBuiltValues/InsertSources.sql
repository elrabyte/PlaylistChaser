CREATE PROCEDURE [dbo].[InsertSources]
AS
set identity_insert dbo.Source on;

insert into dbo.Source (id, name) 
	 values (1, 'Youtube'),
			(2, 'Spotify')

set identity_insert dbo.Source off;