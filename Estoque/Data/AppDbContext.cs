using Estoque.Models;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Produto> Produtos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Produto>(entity =>
    {
        entity.ToTable("PRODUTO");

        entity.HasKey(p => p.Id);

        entity.Property(p => p.Id)
            .HasColumnName("ID");

        entity.Property(p => p.Codigo)
            .HasColumnName("CODIGO")
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(p => p.Codigo)
            .IsUnique();

        entity.Property(p => p.Descricao)
            .HasColumnName("DESCRICAO")
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(p => p.Saldo)
            .HasColumnName("SALDO")
            .IsRequired();

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PRODUTO_SALDO",
                "\"SALDO\" >= 0"
            );
        });
    });
}
}