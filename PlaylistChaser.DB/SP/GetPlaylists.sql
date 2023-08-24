CREATE PROCEDURE [dbo].[GetPlaylists]
	@playlistId int = null
AS
BEGIN
	set nocount on;

	create table #playlist (PlaylistId int, Name nvarchar(max), Description nvarchar(max), AuthorName nvarchar(max), PlaylistTypeId int, PlaylistTypeName nvarchar(255), ThumbnailId int);

	insert into #playlist (PlaylistId, Name, Description, AuthorName, PlaylistTypeId, PlaylistTypeName, ThumbnailId)
		 select p.Id,p.Name,p.Description, p.ChannelName, pt.Id, pt.name, p.ThumbnailId
		   from Playlist p
		  inner join dbo.PlaylistType pt
			 on pt.Id = p.PlaylistTypeId
		  where @playlistId is null or p.Id = @playlistId

	select * from #playlist		
END;
