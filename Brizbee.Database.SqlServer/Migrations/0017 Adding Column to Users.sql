
-- Add column to indicate whether or not to receive Email
ALTER TABLE [dbo].[Users]
	ADD [ShouldSendMidnightPunchEmail] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_ShouldSendMidnightPunchEmail]
	DEFAULT 0 FOR [ShouldSendMidnightPunchEmail];
