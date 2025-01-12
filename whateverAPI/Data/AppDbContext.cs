using Microsoft.EntityFrameworkCore;
using whateverAPI.Entities;
using whateverAPI.Helpers;

namespace whateverAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Joke> Jokes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    // public DbSet<JokeTag> JokeTags { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Joke>()
            .HasMany(j => j.Tags)
            .WithMany()//t => t.Jokes)
            .UsingEntity(j => j.ToTable(Helper.JokeTagsTableName));

        base.OnModelCreating(modelBuilder);
    }
}