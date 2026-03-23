---
description: 'Domain aggregate patterns including mandatory child entities, invariants, and aggregate root design'
applyTo: 'backend/src/PointMasterGame.Domain/**/*.cs'
---

# Domain Aggregate Patterns

Guidelines for designing and implementing domain aggregates following Domain-Driven Design (DDD) principles, including patterns for mandatory child entities, invariants, and aggregate root responsibilities.

## Project Context

- Architecture: Clean Architecture with CQRS
- Domain Layer: Pure business logic with no infrastructure dependencies
- Aggregate Pattern: DDD-style aggregates with strong encapsulation
- Value Objects: Immutable value types for domain concepts
- Invariants: Business rules enforced at the aggregate boundary

## Aggregate Root Fundamentals

### Aggregate Root Responsibilities

An aggregate root is the single entry point for modifying its bounded context. It must:

- **Enforce all business invariants** within its boundary
- **Own the lifecycle** of all entities within the aggregate
- **Prevent direct access** to child entities from outside
- **Expose read-only collections** for child entities
- **Provide methods** that encapsulate all state changes

### Aggregate Root Base Class

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

## Mandatory Child Entities Pattern

### When to Use

Use mandatory child entities when:

- The aggregate **cannot exist** in a valid state without specific child data
- Child entities represent **required configuration** or **default values**
- The business domain requires **all combinations** of a matrix (e.g., all providers × all bet types)
- Omitting child entities would violate **core business invariants**

### Example: IntakeLimit in UserAccount

```csharp
public sealed class UserAccount : AggregateRoot<UserId>
{
    private readonly List<IntakeLimit> _intakeLimits = [];

    /// <summary>
    /// Intake limits for all lottery providers and bet types.
    /// INVARIANT: Must contain entries for all lottery providers and all bet types.
    /// Default limit is 0.
    /// </summary>
    public IReadOnlyCollection<IntakeLimit> IntakeLimits => _intakeLimits.AsReadOnly();

    private UserAccount() { } // EF Core constructor

    /// <summary>
    /// Creates a new user account with mandatory intake limits.
    /// </summary>
    public static UserAccount Create(
        UserId id,
        LoginId loginId,
        /* other parameters */)
    {
        var account = new UserAccount
        {
            Id = id,
            LoginId = loginId,
            /* other properties */
        };

        // MANDATORY: Initialize intake limits for ALL providers and bet types
        account.InitializeIntakeLimits();

        return account;
    }

    /// <summary>
    /// Initializes intake limits for all lottery providers and bet types with default value 0.
    /// This ensures the invariant that all accounts have complete intake limit coverage.
    /// </summary>
    private void InitializeIntakeLimits()
    {
        var providers = Enum.GetValues<LotteryProviderType>();
        var betTypes = Enum.GetValues<BetType>();

        foreach (var provider in providers)
        {
            foreach (var betType in betTypes)
            {
                _intakeLimits.Add(new IntakeLimit
                {
                    LotteryProvider = provider,
                    BetType = betType,
                    Limit = 0m
                });
            }
        }
    }

    /// <summary>
    /// Updates a specific intake limit.
    /// </summary>
    public void UpdateIntakeLimit(
        LotteryProviderType provider,
        BetType betType,
        decimal limit)
    {
        var intakeLimit = _intakeLimits
            .FirstOrDefault(il => il.LotteryProvider == provider && il.BetType == betType);

        if (intakeLimit is null)
        {
            throw new InvalidOperationException(
                $"Intake limit not found for provider '{provider}' and bet type '{betType}'. " +
                "All accounts must have intake limits for all providers and bet types.");
        }

        intakeLimit.Limit = limit;
    }
}
```

### Key Principles

1. **Initialize in Factory Method** - Create mandatory child entities in the aggregate's factory method (e.g., `Create()`)
2. **Private Initialization** - Use private methods to encapsulate the initialization logic
3. **Default Values** - Provide sensible defaults (often 0 or null/empty)
4. **Document Invariants** - Use XML comments to explain the invariant and why it exists
5. **Prevent Partial States** - Never allow the aggregate to exist without complete child data
6. **Update Through Aggregate** - All modifications go through aggregate root methods, not direct collection access

## Invariant Enforcement

### Types of Invariants

1. **Structural Invariants** - Required child entities, relationships, data completeness
2. **Business Rule Invariants** - Domain constraints (e.g., credit limit < intake limit)
3. **Consistency Invariants** - Aggregated values must match sums of parts

### Invariant Validation Pattern

```csharp
public sealed class UserAccount : AggregateRoot<UserId>
{
    /// <summary>
    /// Validates that all required lottery providers have intake limits defined.
    /// </summary>
    private void ValidateIntakeLimits()
    {
        var providers = Enum.GetValues<LotteryProviderType>();
        var betTypes = Enum.GetValues<BetType>();

        var expectedCount = providers.Length * betTypes.Length;
        var actualCount = _intakeLimits.Count;

        if (actualCount != expectedCount)
        {
            throw new DomainException(
                $"UserAccount must have intake limits for all providers and bet types. " +
                $"Expected {expectedCount}, found {actualCount}.");
        }

        // Additional validation: ensure no duplicates
        var uniqueCombinations = _intakeLimits
            .Select(il => (il.LotteryProvider, il.BetType))
            .Distinct()
            .Count();

        if (uniqueCombinations != actualCount)
        {
            throw new DomainException(
                "UserAccount has duplicate intake limit entries for the same provider and bet type.");
        }
    }
}
```

### When to Validate

- **On Creation** - In factory methods after initialization
- **After Modifications** - At the end of update methods
- **On Deserialization** - In private EF Core constructor (optional, for defensive programming)

## Child Entity Encapsulation

### Expose Read-Only Collections

```csharp
private readonly List<IntakeLimit> _intakeLimits = [];
public IReadOnlyCollection<IntakeLimit> IntakeLimits => _intakeLimits.AsReadOnly();
```

### Prevent Direct Mutation

❌ **Bad - Direct collection access:**

```csharp
// Violates encapsulation
account.IntakeLimits.Add(new IntakeLimit { ... });
```

✅ **Good - Through aggregate method:**

```csharp
// Enforces invariants
account.UpdateIntakeLimit(provider, betType, limit);
```

### Update Patterns

```csharp
/// <summary>
/// Updates multiple intake limits in a single operation.
/// </summary>
public void UpdateIntakeLimits(IEnumerable<(LotteryProviderType Provider, BetType BetType, decimal Limit)> updates)
{
    foreach (var (provider, betType, limit) in updates)
    {
        var intakeLimit = _intakeLimits
            .FirstOrDefault(il => il.LotteryProvider == provider && il.BetType == betType);

        if (intakeLimit is null)
        {
            throw new InvalidOperationException(
                $"Cannot update non-existent intake limit for {provider}/{betType}");
        }

        intakeLimit.Limit = limit;
    }

    RaiseDomainEvent(new IntakeLimitsUpdatedDomainEvent(Id));
}
```

## Factory Method Pattern

### Static Factory Methods

Prefer static factory methods over public constructors for aggregate creation:

```csharp
private UserAccount() { } // EF Core only

public static UserAccount Create(
    UserId id,
    LoginId loginId,
    string fullName,
    /* other parameters */)
{
    var account = new UserAccount
    {
        Id = id,
        LoginId = loginId,
        FullName = fullName,
        CreatedAt = DateTime.UtcNow
    };

    // Initialize mandatory child entities
    account.InitializeIntakeLimits();

    // Raise domain event
    account.RaiseDomainEvent(new UserAccountCreatedDomainEvent(id));

    return account;
}
```

### Benefits

- **Clear intent** - Method name describes what's being created
- **Validation** - Can validate parameters before construction
- **Initialization** - Ensures mandatory child entities are created
- **Domain events** - Raises creation events
- **Immutability** - Can return fully-formed, valid aggregate

## Testing Aggregate Invariants

### Unit Test Pattern

```csharp
public sealed class UserAccountIntakeLimitTests
{
    [Fact]
    public void Create_ShouldInitializeIntakeLimitsForAllProvidersAndBetTypes()
    {
        // Arrange
        var providers = Enum.GetValues<LotteryProviderType>();
        var betTypes = Enum.GetValues<BetType>();
        var expectedCount = providers.Length * betTypes.Length;

        // Act
        var account = UserAccount.Create(
            UserId.CreateUnique(),
            LoginId.From("testuser"),
            "Test User");

        // Assert
        account.IntakeLimits.Should().HaveCount(expectedCount);

        foreach (var provider in providers)
        {
            foreach (var betType in betTypes)
            {
                account.IntakeLimits.Should().Contain(il =>
                    il.LotteryProvider == provider &&
                    il.BetType == betType &&
                    il.Limit == 0m);
            }
        }
    }

    [Fact]
    public void UpdateIntakeLimit_WithNonExistentProviderBetType_ShouldThrow()
    {
        // This test ensures the invariant is maintained -
        // you can't create "holes" by updating non-existent entries
        var account = UserAccount.Create(UserId.CreateUnique(), LoginId.From("test"), "Test");

        // This should never happen in practice because all combinations exist
        // but testing the guard clause
        Action act = () => account.UpdateIntakeLimit(
            (LotteryProviderType)999,
            (BetType)999,
            100m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*All accounts must have intake limits*");
    }
}
```

## Common Patterns Summary

| Pattern | When to Use | Key Benefit |
|---------|-------------|-------------|
| Mandatory Child Entities | Aggregate requires complete set of configuration/data | Prevents invalid partial states |
| Factory Methods | Creating aggregates with complex initialization | Ensures all invariants met at creation |
| Private Initialization | Setting up mandatory child entities | Encapsulates complexity |
| Read-Only Collections | Exposing child entities | Prevents external mutation |
| Update Through Methods | Modifying child entities | Enforces invariants on every change |
| Validation Methods | Checking invariants | Centralizes validation logic |

## Anti-Patterns to Avoid

❌ **Lazy Initialization of Mandatory Data**

```csharp
// Bad: Optional initialization
public void InitializeIntakeLimits() { }

// Someone might forget to call it
var account = new UserAccount();
// account.IntakeLimits is empty - invariant violated!
```

✅ **Eager Initialization in Factory**

```csharp
// Good: Initialization is part of creation
public static UserAccount Create(...)
{
    var account = new UserAccount();
    account.InitializeIntakeLimits(); // Always happens
    return account;
}
```

❌ **Public Setters on Collections**

```csharp
// Bad: Can be set to null or empty
public List<IntakeLimit> IntakeLimits { get; set; }
```

✅ **Private Backing Field with Read-Only Exposure**

```csharp
// Good: Controlled access
private readonly List<IntakeLimit> _intakeLimits = [];
public IReadOnlyCollection<IntakeLimit> IntakeLimits => _intakeLimits.AsReadOnly();
```

❌ **Silent Failures**

```csharp
// Bad: Silently does nothing if not found
public void UpdateIntakeLimit(LotteryProviderType provider, BetType betType, decimal limit)
{
    var item = _intakeLimits.FirstOrDefault(il => il.LotteryProvider == provider);
    item?.Limit = limit; // Fails silently if null
}
```

✅ **Explicit Failures**

```csharp
// Good: Throws if invariant violated
public void UpdateIntakeLimit(LotteryProviderType provider, BetType betType, decimal limit)
{
    var item = _intakeLimits.FirstOrDefault(il => il.LotteryProvider == provider);
    if (item is null)
    {
        throw new InvalidOperationException(
            $"Intake limit not found. All accounts must have all limits defined.");
    }
    item.Limit = limit;
}
```

## See Also

- [EF Core Configuration](ef-core-configuration.md) - Persisting aggregates
- [Command Implementation](command-implementation.md) - Modifying aggregates via commands
- [Query Implementation](query-implementation.md) - Reading aggregate data
- AGENTS.md - "Aggregate Root is the Source of Truth" section

## Validation Checklist

When implementing mandatory child entities:

- [ ] Initialize child entities in factory method
- [ ] Use private initialization method for complex setup
- [ ] Expose child entities through read-only collection
- [ ] Provide update methods that enforce invariants
- [ ] Document invariants in XML comments
- [ ] Add validation method that checks invariants
- [ ] Write unit tests for invariant enforcement
- [ ] Test factory method creates complete data
- [ ] Test update methods validate existence
- [ ] Ensure EF Core configuration preserves data