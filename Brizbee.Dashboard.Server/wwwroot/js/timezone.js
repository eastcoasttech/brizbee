window.getTimeZoneId = () => {
    // Returns IANA time zone ID (e.g., "America/Los_Angeles")
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
};