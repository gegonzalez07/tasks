using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tasks.Application;

namespace Tasks.UnitTests.Application;

/// <summary>
/// Guards how validators are built at runtime. FastEndpoints creates them with ActivatorUtilities,
/// which throws "Multiple constructors accepting all given argument types" when a validator has more
/// than one constructor it could use. That only showed up as HTTP 500s, so it is checked here.
/// </summary>
public sealed class ValidatorConventionsTests
{
    public static TheoryData<Type> Validators()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(DependencyInjection).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Validators))]
    public void Validator_HasExactlyOnePublicConstructor(Type validatorType)
    {
        Assert.Single(validatorType.GetConstructors());
    }

    [Theory]
    [MemberData(nameof(Validators))]
    public void Validator_CanBeCreatedWithActivatorUtilitiesLikeFastEndpoints(Type validatorType)
    {
        // Same registrations as the API, so a validator dependency that is not registered also fails here.
        using var provider = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();

        var factory = ActivatorUtilities.CreateFactory(validatorType, Type.EmptyTypes);
        var validator = factory(provider, null);

        Assert.IsAssignableFrom<IValidator>(validator);
    }
}
