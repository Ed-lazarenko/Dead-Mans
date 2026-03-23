namespace backend.Data.Entities;

public class User
{
    public Guid Id { get; set; }

    public string TwitchUserId { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool? EmailVerified { get; set; }

    public string? ProfileImageUrl { get; set; }

    public string? BroadcasterType { get; set; }

    public string? TwitchUserType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<UserRole> AssignedRoles { get; set; } = new List<UserRole>();
}
