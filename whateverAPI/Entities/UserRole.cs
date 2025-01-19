using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace whateverAPI.Entities;

public class UserRole
{
    [Key, Column(Order = 0)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid UserId { get; set; }

    [Key, Column(Order = 1)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid RoleId { get; set; }

    // Navigation properties to both sides of the relationship
    [ForeignKey(nameof(UserId))] public User User { get; set; } = null;

    [ForeignKey(nameof(RoleId))] public Role Role { get; set; } = null;
}