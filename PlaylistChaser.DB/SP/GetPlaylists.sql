CREATE PROCEDURE [dbo].[GetPlaylists]
	@playlistId int = null
AS
BEGIN
	set nocount on;

	create table #playlist (PlaylistId int, Name nvarchar(max), Description nvarchar(max), AuthorName nvarchar(max), PlaylistTypeId int, PlaylistTypeName nvarchar(255), ThumbnailId int, ThumbnailBase64String nvarchar(max));

	insert into #playlist (PlaylistId, Name, Description, AuthorName, PlaylistTypeId, PlaylistTypeName, ThumbnailId)
		 select p.Id,p.Name,p.Description, p.ChannelName, pt.Id, pt.name, p.ThumbnailId
		   from Playlist p
		  inner join dbo.PlaylistType pt
			 on pt.Id = p.PlaylistTypeId
		  where @playlistId is null or p.Id = @playlistId 

	update tmp
	   set ThumbnailBase64String = t.Base64String
	  from #playlist tmp
	inner join dbo.Thumbnail t
	on t.Id = tmp.ThumbnailId

	select * from #playlist		
END;
