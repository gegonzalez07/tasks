using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace Tasks.UnitTests.Common;

/// <summary>
/// Supplies AutoFixture-generated parameters; interfaces and abstract classes are auto-mocked with Moq.
/// Combine with <c>[Frozen]</c> to share a mock between the test and the system under test.
/// </summary>
public sealed class AutoMoqDataAttribute()
    : AutoDataAttribute(() => new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true }));
