-- Index to improve performance of punches endpoint
CREATE NONCLUSTERED INDEX [IX_UserIdInAt]
    ON [dbo].[Punches] ([UserId], [InAt])
    INCLUDE ([CommitId], [CreatedAt], [Guid], [InAtTimeZone],
        [LatitudeForInAt], [LongitudeForInAt], [LatitudeForOutAt],
        [LongitudeForOutAt], [OutAt], [OutAtTimeZone], [TaskId],
        [InAtSourceHardware], [InAtSourceHostname],
        [InAtSourceIpAddress], [InAtSourceOperatingSystem],
        [InAtSourceOperatingSystemVersion], [InAtSourceBrowser],
        [InAtSourceBrowserVersion], [InAtSourcePhoneNumber],
        [OutAtSourceHardware], [OutAtSourceHostname],
        [OutAtSourceIpAddress], [OutAtSourceOperatingSystem],
        [OutAtSourceOperatingSystemVersion], [OutAtSourceBrowser],
        [OutAtSourceBrowserVersion], [OutAtSourcePhoneNumber],
        [ServiceRateId], [PayrollRateId]);
