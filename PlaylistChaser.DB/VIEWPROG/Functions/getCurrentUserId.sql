CREATE FUNCTION [viewprog].[getCurrentUserId]
(
)
RETURNS int 
as
begin
	declare @userId int;
	SELECT @userId = Id 
	  from viewprog.AspNetUsers
	 where DbUserName = current_user

	 return @userId;
end