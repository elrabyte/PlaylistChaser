set identity_insert dbo.State on;

insert into dbo.State (id, name) 
	 values (4, 'NotAdded')

set identity_insert dbo.State off;

--21.08.23