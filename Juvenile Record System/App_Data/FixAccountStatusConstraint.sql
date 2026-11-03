IF EXISTS
(
	SELECT 1
	FROM sys.check_constraints
	WHERE name = N'CK__users__account_s__6754599E'
	  AND parent_object_id = OBJECT_ID(N'dbo.users')
)
BEGIN
	ALTER TABLE dbo.users
		DROP CONSTRAINT [CK__users__account_s__6754599E];
END
GO

ALTER TABLE dbo.users
	ADD CONSTRAINT [CK_users_account_status]
	CHECK ([account_status] IN (N'Active', N'Inactive'));
GO

-- Verify existing rows before returning control to the application.
SELECT [user_id], [account_status]
FROM dbo.users
WHERE [account_status] NOT IN (N'Active', N'Inactive')
   OR [account_status] IS NULL;
GO
