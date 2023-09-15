CREATE PROCEDURE [dbo].[GetPlaylists]
	@playlistId int = null
AS
BEGIN
	set nocount on;

	create table #playlist (Id int, Name nvarchar(max), Description nvarchar(max), ChannelName nvarchar(max), PlaylistTypeId int, PlaylistTypeName nvarchar(255), ThumbnailId int, SongsTotal int);

	insert into #playlist (Id, Name, Description, ChannelName, PlaylistTypeId, PlaylistTypeName, ThumbnailId)
		 select p.Id,p.Name,p.Description, p.ChannelName, pt.Id, pt.name, p.ThumbnailId
		   from Playlist p
		  inner join dbo.PlaylistType pt
			 on pt.Id = p.PlaylistTypeId
		  where @playlistId is null or p.Id = @playlistId

	update tmp 
	   set SongsTotal = ps.SongsTotal
	  from #playlist tmp
	 inner join (select PlaylistId, count(*) SongsTotal 
				   from PlaylistSong 
				  group by PlaylistId) ps
		on ps.PlaylistId = tmp.Id

	select * from #playlist		
END;
