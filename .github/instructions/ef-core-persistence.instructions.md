---
description: 'EF Core persistence layer conventions and patterns for entity configuration, repositories, value converters, and data seeding'
applyTo: 'backend/src/PointMasterGame.Infrastructure/Persistence/**/*.cs'
---

# EF Core Persistence Layer Guidelines

Standards and patterns for the EF Core persistence infrastructure, including entity configurations, repository implementations, value converters, and data seeding.

## Directory Structure

The `Persistence/` folder is organized by domain bounded context:

```
Persistence/
├── ApplicationDbContext.cs          # Main DbContext (implements IUnitOfWork)
├── Common/                           # Shared infrastructure
│   ├── Repository.cs                 # Generic repository base class
│   ├── ProviderCodeConverter.cs      # Global value converter
│   ├── BetTypeConverter.cs           # Value converter
│   └── LotteryProviderConverter.cs   # Value converter
├── Account/                          # Account domain
│   ├── Configurations/               # IEntityTypeConfiguration<T> classes
│   │   ├── UserAccountConfiguration.cs
│   │   └── SubUserConfiguration.cs
│   ├── Repositories/                 # Domain repository implementations
│   │   ├── UserAccountRepository.cs
│   │   └── SubUserRepository.cs
│   └── UserAccountSeeder.cs          # Data seeders (at domain folder level)
├── Betting/                          # Betting domain
│   ├── Configurations/
│   ├── Repositories/
│   └── DrawSeeder.cs
├── Seeding/                          # Seeding infrastructure
│   ├── Abstractions/                 # IDataSeeder, SeederMode, SeederPriority
│   ├── Core/                         # SeederContext, DataSeederOrchestrator
│   ├── Default/                      # Production seeders
│   └── Demo/                         # Demo/test data seeders
└── Migrations/                       # EF Core migrations
```

### Folder Organization Rules

- Each domain gets its own subfolder (e.g., `Account/`, `Betting/`, `Report/`)
- Within each domain folder:
  - `Configurations/` - Entity type configurations
  - `Repositories/` - Repository implementations
  - `*Seeder.cs` files live at the domain folder root (not in a subfolder)
- `Common/` holds shared base classes and global converters
- `Seeding/` contains seeding infrastructure (abstractions, orchestration)

## Entity Configuration Patterns

### File Naming

- Use `{EntityName}Configuration.cs` naming convention
- Place in `{Domain}/Configurations/` folder

### Configuration Class Structure

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointMasterGame.Domain.{Domain}.Aggregates;

namespace PointMasterGame.Infrastructure.Persistence.{Domain}.Configurations;

/// <summary>
/// EF Core configuration for {EntityName} aggregate.
/// </summary>
internal sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        // 1. Table name (snake_case)
        builder.ToTable("table_name");

        // 2. Primary key with value object conversion
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => EntityId.From(value));

        // 3. Required properties
        // 4. Optional properties
        // 5. Owned entities (value objects)
        // 6. JSON columns
        // 7. Relationships
        // 8. Indexes
        // 9. Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}
```

### Column Naming Convention

- Use `snake_case` for all column names via `HasColumnName()`
- Match table names in `snake_case` plural form

```csharp
builder.ToTable("user_accounts");

builder.Property(u => u.LoginId)
    .HasColumnName("login_id")
    .HasMaxLength(50)
    .IsRequired();
```

### Value Object Conversion (Inline)

For strongly-typed IDs and simple value objects, use inline conversion:

```csharp
// Strongly-typed ID
builder.Property(u => u.Id)
    .HasColumnName("id")
    .HasConversion(
        id => id.Value,
        value => UserId.From(value));

// Nullable strongly-typed ID
builder.Property(u => u.ParentUserId)
    .HasColumnName("parent_user_id")
    .HasConversion(
        id => id.HasValue ? id.Value.Value : (Guid?)null,
        value => value.HasValue ? UserId.From(value.Value) : null);

// Simple value object
builder.Property(u => u.LoginId)
    .HasColumnName("login_id")
    .HasConversion(
        loginId => loginId.Value,
        value => LoginId.FromDatabase(value));
```

### Owned Entities (Complex Value Objects)

For complex value objects, use `OwnsOne` with either table columns or JSON:

```csharp
// Flattened into parent table
builder.OwnsOne(u => u.ContactInfo, contact =>
{
    contact.Property(c => c.MobileNumber)
        .HasColumnName("mobile_number")
        .HasMaxLength(20);

    contact.Property(c => c.Email)
        .HasColumnName("email")
        .HasMaxLength(254);
});

// Separate table for owned entity
builder.OwnsOne(u => u.Settings, settings =>
{
    settings.ToTable("user_settings");
    settings.WithOwner().HasForeignKey("UserAccountId");

    settings.Property<Guid>("Id")
        .HasColumnName("id")
        .ValueGeneratedOnAdd();

    settings.HasKey("Id");

    settings.Property(s => s.BetMethod).HasColumnName("bet_method");
});

// Owned entity collection stored in separate table
builder.OwnsMany(u => u.IntakeLimits, intakeLimits =>
{
    intakeLimits.ToTable("user_account_intake_limits");
    intakeLimits.WithOwner().HasForeignKey("UserAccountId");

    intakeLimits.Property<Guid>("Id")
        .HasColumnName("id")
        .ValueGeneratedOnAdd();

    intakeLimits.HasKey("Id");

    intakeLimits.Property(il => il.GameCategory)
        .HasColumnName("game_category")
        .IsRequired();

    intakeLimits.Property(il => il.MaxIntakeAmount)
        .HasColumnName("max_intake_amount")
        .HasPrecision(18, 2)
        .IsRequired();
});
```

### JSON Column Patterns

For complex nested structures, use JSON columns with `ToJson()`:

```csharp
// Owned entity as JSON
builder.OwnsOne(u => u.PositionTaking, pt =>
{
    pt.ToJson("position_taking");

    pt.OwnsOne(p => p.Standard, s =>
    {
        s.Property(x => x.FourD3D);
        s.Property(x => x.FiveD6D);
    });
});

// Collection as JSON
builder.OwnsMany(d => d.Results, results =>
{
    results.ToJson("results");
    results.Property(r => r.Position);
    results.Property(r => r.Number).HasMaxLength(10);
});
```

### JSON Array with ValueComparer

For `IReadOnlyCollection<T>` stored as JSON, always include a `ValueComparer`:

```csharp
builder.Property(u => u.AllowedProviders)
    .HasColumnName("allowed_providers")
    .HasColumnType("jsonb")
    .HasDefaultValue(new List<string>())
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
        new ValueComparer<IReadOnlyCollection<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()));
```

For complex types with enums, use custom `JsonSerializerOptions`:

```csharp
private static readonly JsonSerializerOptions IntakeLimitsJsonOptions = new()
{
    Converters = { new JsonStringEnumConverter() },
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

builder.Property(u => u.IntakeLimits)
    .HasColumnName("intake_limits")
    .HasColumnType("jsonb")
    .HasDefaultValue(new List<IntakeLimit>())
    .HasConversion(
        v => JsonSerializer.Serialize(v, IntakeLimitsJsonOptions),
        v => JsonSerializer.Deserialize<List<IntakeLimit>>(v, IntakeLimitsJsonOptions) ?? new List<IntakeLimit>(),
        new ValueComparer<IReadOnlyCollection<IntakeLimit>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()));
```

### Relationship Configuration

```csharp
// One-to-many with cascade delete
builder.HasMany(b => b.Entries)
    .WithOne()
    .HasForeignKey(e => e.BetSlipId)
    .OnDelete(DeleteBehavior.Cascade);

// Self-referencing with nullable FK
builder.HasIndex(u => u.ParentUserId);

// Composite index with filter
builder.HasIndex(b => new { b.ClientReference, b.AccountCode })
    .HasFilter("client_reference IS NOT NULL");
```

## Repository Implementation Patterns

### Base Repository

All repositories extend `Repository<TEntity, TId>` which provides:

- `DbSet`, `Query`, `ReadOnlyQuery` protected properties
- Standard CRUD operations
- Pagination with `PaginatedResult<T>`
- Dynamic filtering and sorting via `QueryParameters`

### Repository Class Structure

```csharp
using Microsoft.EntityFrameworkCore;
using PointMasterGame.Domain.{Domain}.Aggregates;
using PointMasterGame.Domain.{Domain}.Repositories;
using PointMasterGame.Domain.{Domain}.ValueObjects;
using PointMasterGame.Infrastructure.Persistence.Common;

namespace PointMasterGame.Infrastructure.Persistence.{Domain}.Repositories;

/// <summary>
/// EF Core implementation of I{EntityName}Repository.
/// Extends the generic Repository base class for standard CRUD operations
/// and implements domain-specific query methods.
/// </summary>
internal sealed class {EntityName}Repository : Repository<{EntityName}, {EntityId}>, I{EntityName}Repository
{
    public {EntityName}Repository(ApplicationDbContext context) : base(context) { }

    /// <inheritdoc />
    protected override IQueryable<{EntityName}> ApplyDefaultOrdering(IQueryable<{EntityName}> query)
    {
        return query.OrderByDescending(e => e.CreatedAt);
    }

    // Domain-specific methods
    public async Task<{EntityName}?> GetByXxxAsync(
        XxxId xxxId,
        CancellationToken cancellationToken = default)
    {
        return await ReadOnlyQuery
            .FirstOrDefaultAsync(e => e.XxxId == xxxId, cancellationToken);
    }
}
```

### Repository Guidelines

- Use `internal sealed` access modifier
- Override `ApplyDefaultOrdering` for consistent default sort
- Override `ApplyFilter` for value object filter handling
- Use `ReadOnlyQuery` (with `AsNoTracking()`) for read-only operations
- Use `DbSet` or `Query` when tracking is needed
- Include related entities via `Include()` in specific methods

### Filtering Value Objects

When filtering by value objects with EF Core value conversion:

```csharp
protected override IQueryable<UserAccount> ApplyFilter(IQueryable<UserAccount> query, FilterQuery filter)
{
    if (string.Equals(filter.PropertyName, "LoginId", StringComparison.OrdinalIgnoreCase) &&
        filter.Value is string loginIdValue)
    {
        var normalizedValue = loginIdValue.ToLowerInvariant();

        return filter.Operator switch
        {
            FilterOperator.Equals => query.Where(u => u.LoginId == new LoginId(normalizedValue)),
            FilterOperator.Contains => ApplyLoginIdLikeFilter(query, $"%{normalizedValue}%"),
            _ => query
        };
    }

    return base.ApplyFilter(query, filter);
}
```

## Value Converter Patterns

### Global Value Converters

Register global converters in `ApplicationDbContext.ConfigureConventions()`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<ProviderCode>()
        .HaveConversion<ProviderCodeConverter>();
}
```

### Value Converter Class

Place in `Common/` folder:

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PointMasterGame.Domain.Common.ValueObjects;

namespace PointMasterGame.Infrastructure.Persistence.Common;

/// <summary>
/// Value converter for {ValueObject} to/from {PrimitiveType}.
/// </summary>
public sealed class {ValueObject}Converter : ValueConverter<{ValueObject}, {PrimitiveType}>
{
    public {ValueObject}Converter()
        : base(
            vo => vo.Value,
            value => {ValueObject}.From(value))
    {
    }
}
```

### Nullable Value Converter Variant

```csharp
public sealed class LotteryProviderConverter : ValueConverter<LotteryProvider?, string?>
{
    public LotteryProviderConverter()
        : base(
            provider => provider != null ? provider.Code : null,
            code => code != null ? LotteryProvider.FromCode(code) : null)
    {
    }
}
```

## Data Seeding Patterns

### Seeder Interface

Implement `IDataSeeder` for automatic discovery:

```csharp
public interface IDataSeeder
{
    string Name { get; }
    SeederMode SupportedModes { get; }
    int Priority { get; }
    IReadOnlyList<string> Dependencies { get; }
    Task SeedAsync(SeederContext context, CancellationToken cancellationToken);
}
```

### Seeder Modes

- `SeederMode.Default` - Production essential data
- `SeederMode.Demo` - Demo/test data
- `SeederMode.Development` - Developer convenience data

### Seeder Priority Constants

Use `SeederPriority` constants for consistent ordering:

```csharp
Priority = SeederPriority.Essential;    // Core reference data
Priority = SeederPriority.Standard;     // Normal domain data
Priority = SeederPriority.Late;         // Dependent on other seeders
```

## ApplicationDbContext Patterns

### DbSet Properties

Group by domain with comments:

```csharp
// Account Domain
public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
public DbSet<SubUser> SubUsers => Set<SubUser>();

// Betting Domain
public DbSet<BetSlip> BetSlips => Set<BetSlip>();
public DbSet<Draw> Draws => Set<Draw>();
```

### Configuration Discovery

Use assembly scanning in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

## Naming Conventions Summary

| Element | Convention | Example |
|---------|------------|---------|
| Table names | snake_case plural | `user_accounts` |
| Column names | snake_case | `login_id`, `created_at` |
| JSON columns | snake_case | `position_taking`, `results` |
| Configuration class | `{Entity}Configuration` | `UserAccountConfiguration` |
| Repository class | `{Entity}Repository` | `UserAccountRepository` |
| Value converter | `{ValueObject}Converter` | `ProviderCodeConverter` |
| Seeder class | `{Entity}Seeder` | `UserAccountSeeder` |

## Common Patterns Checklist

When creating a new entity configuration:

- [ ] Set table name with `ToTable("snake_case_plural")`
- [ ] Configure primary key with value object conversion
- [ ] Use `HasColumnName()` for all properties
- [ ] Configure value objects with inline conversion or `OwnsOne`
- [ ] Use `ToJson()` for complex nested structures
- [ ] Add `ValueComparer` for collection JSON columns
- [ ] Configure relationships with appropriate delete behavior
- [ ] Add indexes for frequently queried properties
- [ ] Add `builder.Ignore(e => e.DomainEvents)` for aggregates
- [ ] Mark class as `internal sealed`
- [ ] Ensure mandatory child entities are persisted (see domain-aggregates.instructions.md)

When creating a new repository:

- [ ] Extend `Repository<TEntity, TId>`
- [ ] Implement domain-specific interface from `Domain` layer
- [ ] Override `ApplyDefaultOrdering`
- [ ] Override `ApplyFilter` if entity has value object filters
- [ ] Use `ReadOnlyQuery` for read operations
- [ ] Include XML documentation
- [ ] Mark class as `internal sealed`

## See Also

- [Domain Aggregates](domain-aggregates.instructions.md) - Aggregate patterns including mandatory child entities
