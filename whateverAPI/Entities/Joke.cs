using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using whateverAPI.Data;

namespace whateverAPI.Entities;

public class Joke : IEntity<Guid>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    public required string Content { get; set; }
    public JokeType? Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Tag>? Tags { get; set; } = [];
    public int? LaughScore { get; set; }
}