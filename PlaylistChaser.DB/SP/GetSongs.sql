CREATE PROCEDURE [dbo].[GetSongs]
	@playlistId int
AS
BEGIN
	set nocount on;

	create table #song (Id int, SongName nvarchar(255), ArtistName nvarchar(255), Downloaded bit, ThumbnailId int, ThumbnailBase64String nvarchar(max));

	insert into #song (Id, SongName, ArtistName, Downloaded, ThumbnailId)
		 select s.Id, isnull(s.SongName, s.YoutubeSongName), s.ArtistName, 0, s.ThumbnailId
		   from Song s
		   inner join PlaylistSong ps
		   on ps.SongId = s.Id
		  where ps.PlaylistId = @playlistId

	update tmp
	   set ThumbnailBase64String = t.Base64String
	  from #song tmp
	 inner join dbo.Thumbnail t
		on t.Id = tmp.ThumbnailId

	select * from #song
END;
