﻿CREATE PROCEDURE [dbo].[GetPlaylistSongs]
	@playlistId int
AS
BEGIN
	set nocount on;

	create table #playlistSong (PlaylistSongId int, SongId int, SongName nvarchar(255), ArtistName nvarchar(255), Downloaded bit, ThumbnailId int);

	insert into #playlistSong (PlaylistSongId, SongId, SongName, ArtistName, Downloaded, ThumbnailId)
		 select ps.Id, s.Id, s.SongName, s.ArtistName, 0, s.ThumbnailId
		   from PlaylistSong ps
		  inner join Song s
			 on s.Id = ps.SongId
		  where ps.PlaylistId = @playlistId

	select * from #playlistSong
END;
