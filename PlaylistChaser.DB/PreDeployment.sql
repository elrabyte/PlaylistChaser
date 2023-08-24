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
--24.08.23