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
        // Set default value for CreatedAt if you want to handle it at the database level
        // modelBuilder.Entity<JokeEntity>()
        //     .Property(e => e.CreatedAt)
        //     .HasDefaultValueSql("GETUTCDATE()");

        // Configure composite key for JokeTag

        modelBuilder.Entity<Joke>()
            .HasMany(j => j.Tags)
            .WithMany(t => t.Jokes)
            .UsingEntity(j => j.ToTable(ProjectHelper.JokeTagsTableName));
        //
        // // Configure relationships
        // modelBuilder.Entity<JokeTag>()
        //     .HasOne(jt => jt.Joke)
        //     .WithMany()
        //     .HasForeignKey(jt => jt.JokeId);
        //
        // modelBuilder.Entity<JokeTag>()
        //     .HasOne(jt => jt.Tag)
        //     .WithMany()
        //     .HasForeignKey(jt => jt.TagId);
        //
        // base.OnModelCreating(modelBuilder);

        // modelBuilder.Entity<Joke>()
        //     .HasMany(j => j.Tags)
        //     .WithMany(t => t.Jokes)
        //     .UsingEntity<JokeTag>(
        //         j => j.HasOne(jt => jt.Tag)
        //             .WithMany(t => t.JokeTags)
        //             .HasForeignKey(jt => jt.TagId),
        //         t => t.HasOne(jt => jt.Joke)
        //             .WithMany(j => j.JokeTags)
        //             .HasForeignKey(jt => jt.JokeId)
        //     );

        base.OnModelCreating(modelBuilder);
    }
}