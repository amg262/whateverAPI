using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace whateverAPI.Entities;

public class Role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public bool IsActive { get; set; }

    // // Many-to-many relationship with users
    public List<User> Users { get; set; } = [];
    
    public static Role Create(string name, string? description = null) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name.ToLower().Trim(),
        Description = description,
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };
}