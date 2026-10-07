using Daas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daas.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Schema> Schemas => Set<Schema>();

    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();

}
