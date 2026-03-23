namespace backend.Data.Entities;

public class UserRole
{
    public Guid UserId { get; set; }

    public short RoleId { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public User User { get; set; } = default!;

    public Role Role { get; set; } = default!;

    public User? AssignedByUser { get; set; }
}
