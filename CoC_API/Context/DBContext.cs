using Microsoft.EntityFrameworkCore;
using CoC_API.Models;

namespace CoC_API.Context;

public class DBContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Campaign> Campaings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("mexer");
    }
}