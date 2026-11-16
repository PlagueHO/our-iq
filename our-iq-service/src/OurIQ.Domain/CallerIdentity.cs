namespace OurIQ.Domain;

public static class CallerIdentity
{
    public static string? NormalizeUserId(string? userId) =>
        Guid.TryParse(userId, out var parsed) ? parsed.ToString("D") : userId;
}
