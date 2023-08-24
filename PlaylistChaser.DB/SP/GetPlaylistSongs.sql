CREATE PROCEDURE [dbo].[GetPlaylistSongs]
	@playlistId int
AS
BEGIN
	set nocount on;

	create table #playlistSong (PlaylistSongId int, SongId int, SongName nvarchar(255), ArtistName nvarchar(255), Downloaded bit, ThumbnailId int, YoutubeId nvarchar(255));

	insert into #playlistSong (PlaylistSongId, SongId, SongName, ArtistName, Downloaded, ThumbnailId, YoutubeId)
		 select ps.Id, s.Id, isnull(s.SongName, s.YoutubeSongName), s.ArtistName, 0, s.ThumbnailId, s.YoutubeId
		   from PlaylistSong ps
		  inner join Song s
			 on s.Id = ps.SongId
		  where ps.PlaylistId = @playlistId

	select * from #playlistSong
END;
