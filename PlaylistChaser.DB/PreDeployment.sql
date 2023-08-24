set identity_insert dbo.State on;

insert into dbo.State (id, name) 
	 values (4, 'NotAdded')

set identity_insert dbo.State off;

--21.08.23

update Playlist 
set ThumbnailId = null

update Song
set ThumbnailId = null

delete from Thumbnail

--change state tabledefinition
alter TABLE [dbo].[State]
add [Entity] VARCHAR(50) NULL

update State 
set Entity = '';

set identity_insert dbo.State on;

insert into dbo.State (id, name, Entity) 
	 values (100, 'NotAvailable', 'Song'),
			(101, 'NotChecked', 'Song'),
			(110, 'Available', 'Song'),
			(200, 'NotAdded', 'PlaylistSong'),
			(210, 'Added', 'PlaylistSong')

set identity_insert dbo.State off;

update PlaylistSongState
set StateId = 200
where StateId = 4

update PlaylistSongState
set StateId = 210
where StateId = 2

delete from State where id <= 4

alter TABLE [dbo].[State]
alter column [Entity] VARCHAR(50) NULL


--add songstates
insert into dbo.State (id, name, Entity)
		values (110, 'Available', 'Song');

insert into SongState (SongId, SourceId, StateId, LastChecked)
select id, 1, 110,getdate() from song


--remove spotify properties on song
alter table Song drop column foundonspotify
alter table Song drop column addedtospotify
ALTER TABLE [dbo].[Song] DROP COLUMN [IsNotOnSpotify];

--24.08.23