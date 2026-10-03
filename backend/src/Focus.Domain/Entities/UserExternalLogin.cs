using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class UserExternalLogin : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public AuthProvider Provider { get; private set; }
    public string ProviderKey { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private UserExternalLogin() { }

    public UserExternalLogin(Guid userId, AuthProvider provider, string providerKey, string email)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }
}
