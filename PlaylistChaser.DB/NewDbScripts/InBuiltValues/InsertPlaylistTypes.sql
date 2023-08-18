CREATE PROCEDURE [dbo].[InsertPlaylistTypes]
AS
set identity_insert dbo.PlaylistType on;

insert into dbo.PlaylistType (id, name) 
	 values (1, 'Simple'),
			(2, 'Combined')

set identity_insert dbo.PlaylistType off;