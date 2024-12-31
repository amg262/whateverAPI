using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace whateverAPI.Entities;
public class JokeTag
{
    [Key, Column(Order = 0)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid JokeId { get; set; }

    [Key, Column(Order = 1)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid TagId { get; set; }

    // Navigation properties to both sides of the relationship
    [ForeignKey(nameof(JokeId))] public Joke Joke { get; set; } = null;

    [ForeignKey(nameof(TagId))] public Tag Tag { get; set; } = null;
}