namespace ECommerceWeb.Time;

public static class VietnamTime
{
    private static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    public static DateTime LocalToUtc(DateTime localUnspecified) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified), Zone);

    public static DateTime UtcToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
}
