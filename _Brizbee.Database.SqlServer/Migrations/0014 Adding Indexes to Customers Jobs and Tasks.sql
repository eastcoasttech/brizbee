-- Index to improve performance of customers endpoint
CREATE NONCLUSTERED INDEX [IX_OrganizationIdNameNumber]
    ON [dbo].[Customers] ([OrganizationId], [Name], [Number]);


-- Index to improve performance of jobs endpoint
CREATE NONCLUSTERED INDEX [IX_CustomerIdNameNumberStatus]
    ON [dbo].[Jobs] ([CustomerId], [Name], [Number], [Status]);


-- Index to improve performance of jobs endpoint
CREATE NONCLUSTERED INDEX [IX_CustomerIdNumberNameStatus]
    ON [dbo].[Jobs] ([CustomerId], [Number], [Name], [Status]);


-- Index to improve performance of tasks endpoint
CREATE NONCLUSTERED INDEX [IX_JobIdNameNumber]
    ON [dbo].[Tasks] ([JobId], [Name], [Number]);
    

-- Index to improve performance of tasks endpoint
CREATE NONCLUSTERED INDEX [IX_JobIdNumberName]
    ON [dbo].[Tasks] ([JobId], [Number], [Name]);
