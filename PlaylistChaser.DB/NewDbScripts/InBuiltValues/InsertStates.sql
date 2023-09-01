CREATE PROCEDURE [dbo].[InsertStates]
AS
set identity_insert [dbo].[State] on;

insert into [dbo].[State] (id, name, Entity) 
	 values (100, 'NotAvailable', 'Song'),
			(101, 'NotChecked', 'Song'),
			(110, 'Available', 'Song'),
			(111, 'MaybeAvailable', 'Song'),

			(200, 'NotAdded', 'PlaylistSong'),
			(210, 'Added', 'PlaylistSong')

set identity_insert [dbo].[State] off;