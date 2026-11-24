using Microsoft.EntityFrameworkCore;
using CoC_API.Models;

namespace CoC_API.Context;

public class CoC_DbContext : DbContext
{
    public CoC_DbContext(DbContextOptions<CoC_DbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // As propriedades do User validam (e a password faz hash) no setter.
        // Para o EF ler/escrever na BD sem passar por essa lógica, mapeamos cada
        // propriedade ao respetivo campo "*Value", que guarda o valor já validado.
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.id).HasField("idValue");
            user.Property(u => u.name).HasField("nameValue");
            user.Property(u => u.email).HasField("emailValue");
            user.Property(u => u.password).HasField("passwordValue");
            user.Property(u => u.birthDate).HasField("birthDateValue");
            user.Property(u => u.taxId).HasField("taxIdValue");
            user.Property(u => u.caracteristics).HasField("caracteristicsValue");
            user.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Campaign>(campaign =>
        {
            campaign.Property(c => c.Types)
                .HasField("types")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UserSession>(session =>
        {
            session.HasKey(s => s.TokenHash);
            session.Property(s => s.TokenHash).HasMaxLength(64);
            session.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
