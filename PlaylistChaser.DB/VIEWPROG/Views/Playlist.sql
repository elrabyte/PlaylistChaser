CREATE VIEW [viewprog].[Playlist]
	AS 
	SELECT * 
	  FROM [dbo].[Playlist]
	 WHERE UserId = viewprog.getCurrentUserId()
