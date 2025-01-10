using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using whateverAPI.Data;

namespace whateverAPI.Entities;

public class Tag : IEntity<Guid>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public required string Name { get; set; } = string.Empty;

    // Navigation property for many-to-many relationship
    // [JsonIgnore] [NotMapped] public List<Joke> Jokes { get; set; } = [];
}