CREATE NONCLUSTERED INDEX [IX_Punches_TaskId]
    ON [dbo].[Punches] ([TaskId]);

CREATE NONCLUSTERED INDEX [IX_Tasks_BaseServiceRateId]
    ON [dbo].[Tasks] ([BaseServiceRateId]);
    
CREATE NONCLUSTERED INDEX [IX_Tasks_BasePayrollRateId]
    ON [dbo].[Tasks] ([BasePayrollRateId]);
    
CREATE NONCLUSTERED INDEX [IX_Rates_ParentRateId]
    ON [dbo].[Rates] ([ParentRateId]);
    
CREATE NONCLUSTERED INDEX [IX_Rates_OrganizationId]
    ON [dbo].[Rates] ([OrganizationId]);
