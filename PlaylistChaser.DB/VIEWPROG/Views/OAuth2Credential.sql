CREATE VIEW [viewprog].[OAuth2Credential]
	AS
	SELECT * 
	  FROM [dbo].[OAuth2Credential]
	 WHERE UserId = viewprog.getCurrentUserId()
