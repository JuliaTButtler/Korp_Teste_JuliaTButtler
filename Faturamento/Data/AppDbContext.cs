using Faturamento.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotaFiscal> NotasFiscais { get; set; }

    public DbSet<ItemNotaFiscal> ItensNotaFiscal { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotaFiscal>(entity =>
        {
            entity.ToTable("NOTA_FISCAL");

            entity.HasKey(n => n.Id);

            entity.Property(n => n.Id)
                .HasColumnName("ID");

            entity.Property(n => n.Numero)
                .HasColumnName("NUMERO")
                .IsRequired();

            entity.HasIndex(n => n.Numero)
                .IsUnique();

            entity.Property(n => n.Status)
                .HasColumnName("STATUS")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusNotaFiscal.ABERTA)
                .IsRequired();

            entity.Property(n => n.DataCriacao)
                .HasColumnName("DATA_CRIACAO")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_NOTA_FISCAL_STATUS",
                    "\"STATUS\" IN (''ABERTA'', ''FECHADA'')"
                );
            });
        });

        modelBuilder.Entity<ItemNotaFiscal>(entity =>
        {
            entity.ToTable("ITEM_NOTA_FISCAL");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.Id)
                .HasColumnName("ID");

            entity.Property(i => i.NotaFiscalId)
                .HasColumnName("NOTA_FISCAL_ID")
                .IsRequired();

            entity.Property(i => i.ProdutoId)
                .HasColumnName("PRODUTO_ID")
                .IsRequired();

            entity.Property(i => i.Quantidade)
                .HasColumnName("QUANTIDADE")
                .IsRequired();

            entity.HasOne(i => i.NotaFiscal)
                .WithMany(n => n.Itens)
                .HasForeignKey(i => i.NotaFiscalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => new { i.NotaFiscalId, i.ProdutoId })
                .IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_ITEM_NOTA_FISCAL_QUANTIDADE",
                    "\"QUANTIDADE\" > 0"
                );
            });
        });
    }
}
