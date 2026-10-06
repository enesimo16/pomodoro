using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class UserDeviceSession : BaseEntity<Guid>
{
    public Guid? UserId { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string DeviceType { get; private set; } = "Desktop";
    public string OperatingSystem { get; private set; } = "Unknown";
    public string Browser { get; private set; } = "Unknown";
    public int LoginCount { get; private set; } = 1;
    public int ActiveMinutes { get; private set; }
    public string? LastPath { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    // Navigation
    public User? User { get; private set; }

    private UserDeviceSession() { }

    public static UserDeviceSession Create(
        Guid? userId,
        string ipAddress,
        string userAgent,
        string deviceType,
        string operatingSystem,
        string browser,
        string? initialPath = null)
    {
        var now = DateTime.UtcNow;
        return new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "Bilinmeyen" : userAgent.Trim(),
            DeviceType = deviceType,
            OperatingSystem = operatingSystem,
            Browser = browser,
            LoginCount = 1,
            ActiveMinutes = 1,
            LastPath = initialPath,
            FirstSeenAt = now,
            LastSeenAt = now
        };
    }

    public void RecordActivity(DateTime now, string? path = null)
    {
        var diff = (now - LastSeenAt).TotalMinutes;
        
        // 30 dakikadan uzun sure sonra geldiyse oturum/ziyaret sayisini artir
        if (diff > 30)
        {
            LoginCount++;
        }
        
        // 15 dakika icindeki aktif isteklerde gecen sureyi aktif dakikaya ekle
        if (diff > 0 && diff <= 15)
        {
            ActiveMinutes += Math.Max(1, (int)Math.Round(diff));
        }

        LastSeenAt = now;
        if (!string.IsNullOrWhiteSpace(path))
        {
            LastPath = path;
        }
    }

    public void AssociateUser(Guid userId)
    {
        UserId = userId;
    }
}
