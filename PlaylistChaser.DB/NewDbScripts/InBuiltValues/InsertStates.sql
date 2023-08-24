CREATE PROCEDURE [dbo].[InsertStates]
AS
set identity_insert dbo.State on;

insert into dbo.State (id, name, Entity) 
	 values (100, 'NotChecked', 'Song'),
			(101, 'NotAvailable', 'Song'),
			(200, 'NotAdded', 'PlaylistSong'),
			(210, 'Added', 'PlaylistSong')

set identity_insert dbo.State off;