CREATE PROCEDURE [VIEWPROG].[GetSongs]
AS
BEGIN
	set nocount on;

	create table #song (Id int, SongName nvarchar(255), ArtistName nvarchar(255), Downloaded bit, ThumbnailId int);

	insert into #song (Id, SongName, ArtistName, Downloaded, ThumbnailId)
		 select s.Id, s.SongName, s.ArtistName, 0, s.ThumbnailId
		   from Song s

	select * from #song
END;
