CREATE PROCEDURE [dbo].[InsertStates]
AS
set identity_insert dbo.State on;

insert into dbo.State (id, name) 
	 values (1, 'NotChecked'),
			(2, 'Added'),
			(3, 'NotAvailable')

set identity_insert dbo.State off;