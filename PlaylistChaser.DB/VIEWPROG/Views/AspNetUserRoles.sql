CREATE VIEW [viewprog].[AspNetUserRoles]
	AS 
	SELECT * 
	  FROM [dbo].[AspNetUserRoles]
	 where UserId = viewprog.getCurrentUserId();