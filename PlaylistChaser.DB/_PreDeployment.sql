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


--SpotifyUrl to SpotifyId
ALTER TABLE [dbo].[Playlist] DROP COLUMN [SpotifyUrl];

--24.08.23

insert into PlaylistAdditionalInfo ([PlaylistId],[SourceId],[PlaylistIdSource],[Name],[CreatorName],[Description],[Url], isMine)
select id, 1, YoutubeId, Name, ChannelName,Description, YoutubeUrl,iif(ChannelName = 'Tatsu',1,0) from Playlist

alter table playlist 
drop column YoutubeUrl
alter table playlist 
drop column YoutubeId

insert into PlaylistAdditionalInfo ([PlaylistId],[SourceId],[PlaylistIdSource],[Name],[CreatorName],[Description], isMine)
select id, 2, SpotifyId, Name, ChannelName,Description,1 from Playlist WHERE SpotifyId IS NOT NULL

alter table playlist 
drop column SpotifyId

--songs info
----youtube
insert into SongAdditionalInfo ([SongId],[SourceId],[SongIdSource],[Name],[ArtistName])
select id, 1, YoutubeId, YoutubeSongName, isnull(ArtistName,YoutubeSongName)  from Song where YoutubeId is not null

alter table Song 
drop column YoutubeId
alter table Song 
drop column YoutubeSongName
----spotify
insert into SongAdditionalInfo ([SongId],[SourceId],[SongIdSource],[Name],[ArtistName])
select id, 2, SpotifyId, SongName, isnull(ArtistName,SongName) from Song where SpotifyId is not null

alter table Song 
drop column SpotifyId
-------

set identity_insert dbo.State on;
insert into dbo.State (id, name, Entity) 
	 values (111, 'MaybeAvailable', 'Song')
	 set identity_insert dbo.State off;

---31.08.23

