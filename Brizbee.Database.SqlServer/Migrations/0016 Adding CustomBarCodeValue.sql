
-- Add column for storing custom bar code value
ALTER TABLE [dbo].[QBDInventoryItems]
ADD [CustomBarCodeValue] VARCHAR(50) NULL;


-- Modify Inventory Items Permission
ALTER TABLE [dbo].[Users]
	ADD [CanModifyInventoryItems] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyInventoryItems]
	DEFAULT 0 FOR [CanModifyInventoryItems];
