CREATE VIEW [viewprog].[AspNetUsers]
	AS 
	SELECT * 
	  FROM [dbo].[AspNetUsers]
	 where DbUserName = current_user;