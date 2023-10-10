CREATE PROCEDURE [VIEWPROG].[MergeSongs]
	@songIds dbo.list_int readonly,
	@mainSongId int = null
AS
BEGIN
	set nocount on;

	
	begin if (@mainSongId is null)
		select @mainSongId = min(id) from  @songIds 
	end;
	
	--link playlist songs to main song
	update PlaylistSong 
	set SongId = @mainSongId
	where SongId in (select id from @songids)

	-- remove other songs
	select id into #otherSongIds from @songIds where id <> @mainSongId

	--	remove state
		delete ss
		  from SongState ss
		 inner join #otherSongIds ids
			on ids.id = ss.SongId
	--	remove info
		delete i 
		  from SongInfo i
		 inner join #otherSongIds ids
			on ids.id = i.SongId
	--	remove song itself
		delete s
		  from song s
		 inner join #otherSongIds ids
			on ids.id = s.Id

	select 0;
END;
