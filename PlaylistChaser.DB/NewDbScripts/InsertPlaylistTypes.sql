CREATE PROCEDURE [dbo].[InsertPlaylistTypes]
AS
set identity_insert dbo.PlaylistTypes on;

insert into dbo.PlaylistTypes (id, name) 
	 values (1, 'Simple'),
			(2, 'Combined')

set identity_insert dbo.PlaylistTypes off;