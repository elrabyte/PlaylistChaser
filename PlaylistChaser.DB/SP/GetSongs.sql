CREATE PROCEDURE [dbo].[GetSongs]
	@playlistId int
AS
BEGIN
	set nocount on;

	create table #song (Id int, SongName nvarchar(255), ArtistName nvarchar(255), Downloaded bit, ThumbnailId int, YoutubeId nvarchar(255));

	insert into #song (Id, SongName, ArtistName, Downloaded, ThumbnailId, YoutubeId)
		 select s.Id, isnull(s.SongName, s.YoutubeSongName), s.ArtistName, 0, s.ThumbnailId, s.YoutubeId
		   from Song s
		  inner join PlaylistSong ps
			 on ps.SongId = s.Id
		  where ps.PlaylistId = @playlistId

	select * from #song
END;
