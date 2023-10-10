CREATE SCHEMA [VIEWPROG]
    AUTHORIZATION [dbo];


GO
GRANT DELETE
    ON SCHEMA::[VIEWPROG] TO [registered_user]
    AS [dbo];


GO
GRANT EXECUTE
    ON SCHEMA::[VIEWPROG] TO [registered_user]
    AS [dbo];


GO
GRANT INSERT
    ON SCHEMA::[VIEWPROG] TO [registered_user]
    AS [dbo];


GO
GRANT SELECT
    ON SCHEMA::[VIEWPROG] TO [registered_user]
    AS [dbo];


GO
GRANT UPDATE
    ON SCHEMA::[VIEWPROG] TO [registered_user]
    AS [dbo];

