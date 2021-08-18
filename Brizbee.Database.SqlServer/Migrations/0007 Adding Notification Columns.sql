
-- Users Table

ALTER TABLE [dbo].[Users]
	ADD [NotificationMobileNumbers] VARCHAR (260) NULL;

ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_NotificationMobileNumbers]
	DEFAULT '' FOR [NotificationMobileNumbers];
