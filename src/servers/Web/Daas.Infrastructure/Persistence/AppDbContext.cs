using Daas.Application.Common.Interfaces;
using Daas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daas.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<Schema> Schemas => Set<Schema>();

    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<User> Users => Set<User>();

    IQueryable<User> IAppDbContext.Users => Users;
}
