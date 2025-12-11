using FluentAssertions;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Tests.ServiceTests;

public class SubscriptionGroupManagerTests
{
    private readonly SubscriptionGroupManager subscriptionGroupManager;

    public SubscriptionGroupManagerTests()
    {
        // Arrange - Initialize the system under test
        subscriptionGroupManager = new SubscriptionGroupManager();
    }

    [Fact]
    public void TryAdd_WithNewSubscription_ReturnsTrue()
    {
        // Arrange
        var subscription = new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act
        var result = subscriptionGroupManager.TryAdd(subscription);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TryAdd_WithDuplicateGuid_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();

        var subscription1 = new SubscriptionGroup
        {
            Guid = guid,
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        var subscription2 = new SubscriptionGroup
        {
            Guid = guid, // Same GUID
            WebSocketUrls = new List<string> { "wss://test.com/stream2" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act
        var firstAdd = subscriptionGroupManager.TryAdd(subscription1);
        var secondAdd = subscriptionGroupManager.TryAdd(subscription2);

        // Assert
        firstAdd.Should().BeTrue();
        secondAdd.Should().BeFalse();
    }

    [Fact]
    public void Get_WithExistingGuid_ReturnsSubscription()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expectedUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" };
        var expectedStartTime = DateTime.UtcNow;
        var expectedExpiryTime = DateTime.UtcNow.AddMinutes(60);

        var subscription = new SubscriptionGroup
        {
            Guid = guid,
            WebSocketUrls = expectedUrls,
            StartedAt = expectedStartTime,
            ExpiresAt = expectedExpiryTime,
            CancellationTokenSource = new CancellationTokenSource()
        };

        subscriptionGroupManager.TryAdd(subscription);

        // Act
        var result = subscriptionGroupManager.Get(guid);

        // Assert
        result.Should().NotBeNull();
        result.Guid.Should().Be(guid);
        result.WebSocketUrls.Should().BeEquivalentTo(expectedUrls);
        result.StartedAt.Should().Be(expectedStartTime);
        result.ExpiresAt.Should().Be(expectedExpiryTime);
    }

    [Fact]
    public void Get_WithNonExistentGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentGuid = Guid.NewGuid();

        // Act
        Action act = () => subscriptionGroupManager.Get(nonExistentGuid);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Subscription does not exist {nonExistentGuid}");
    }

    [Fact]
    public void Get_WithNoParameters_ReturnsEmptyListWhenNoSubscriptions()
    {
        // Arrange
        // No subscriptions added

        // Act
        var result = subscriptionGroupManager.Get();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Get_WithNoParameters_ReturnsAllSubscriptions()
    {
        // Arrange
        var subscription1 = new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        var subscription2 = new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { "wss://test.com/stream2" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = null,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var subscription3 = new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { "wss://test.com/stream3", "wss://test.com/stream4" },
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(55),
            CancellationTokenSource = new CancellationTokenSource()
        };

        subscriptionGroupManager.TryAdd(subscription1);
        subscriptionGroupManager.TryAdd(subscription2);
        subscriptionGroupManager.TryAdd(subscription3);

        // Act
        var result = subscriptionGroupManager.Get();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Guid == subscription1.Guid);
        result.Should().Contain(s => s.Guid == subscription2.Guid);
        result.Should().Contain(s => s.Guid == subscription3.Guid);
    }

    [Fact]
    public void Remove_WithExistingGuid_RemovesSubscription()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var subscription = new SubscriptionGroup
        {
            Guid = guid,
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        subscriptionGroupManager.TryAdd(subscription);

        // Act
        subscriptionGroupManager.Remove(guid);

        // Assert
        var allSubscriptions = subscriptionGroupManager.Get();
        allSubscriptions.Should().NotContain(s => s.Guid == guid);
    }

    [Fact]
    public void Remove_WithNonExistentGuid_DoesNotThrowException()
    {
        // Arrange
        var nonExistentGuid = Guid.NewGuid();

        // Act
        Action act = () => subscriptionGroupManager.Remove(nonExistentGuid);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_AfterRemoval_GetThrowsException()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var subscription = new SubscriptionGroup
        {
            Guid = guid,
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        subscriptionGroupManager.TryAdd(subscription);
        subscriptionGroupManager.Remove(guid);

        // Act
        Action act = () => subscriptionGroupManager.Get(guid);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Subscription does not exist {guid}");
    }

    [Fact]
    public void TryAdd_MultipleSubscriptions_AllAreStoredIndependently()
    {
        // Arrange
        var subscriptions = Enumerable.Range(1, 5).Select(i => new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { $"wss://test.com/stream{i}" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(i * 10),
            CancellationTokenSource = new CancellationTokenSource()
        }).ToList();

        // Act
        var addResults = subscriptions.Select(s => subscriptionGroupManager.TryAdd(s)).ToList();

        // Assert
        addResults.Should().AllSatisfy(result => result.Should().BeTrue());

        foreach (var subscription in subscriptions)
        {
            var retrieved = subscriptionGroupManager.Get(subscription.Guid);
            retrieved.Should().NotBeNull();
            retrieved.Guid.Should().Be(subscription.Guid);
            retrieved.WebSocketUrls.Should().BeEquivalentTo(subscription.WebSocketUrls);
        }
    }

    [Fact]
    public void Remove_FromMultipleSubscriptions_RemovesOnlySpecified()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();

        var subscription1 = new SubscriptionGroup
        {
            Guid = guid1,
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        var subscription2 = new SubscriptionGroup
        {
            Guid = guid2,
            WebSocketUrls = new List<string> { "wss://test.com/stream2" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        var subscription3 = new SubscriptionGroup
        {
            Guid = guid3,
            WebSocketUrls = new List<string> { "wss://test.com/stream3" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = new CancellationTokenSource()
        };

        subscriptionGroupManager.TryAdd(subscription1);
        subscriptionGroupManager.TryAdd(subscription2);
        subscriptionGroupManager.TryAdd(subscription3);

        // Act
        subscriptionGroupManager.Remove(guid2);

        // Assert
        var remaining = subscriptionGroupManager.Get();
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(s => s.Guid == guid1);
        remaining.Should().Contain(s => s.Guid == guid3);
        remaining.Should().NotContain(s => s.Guid == guid2);
    }

    [Fact]
    public void Get_WithGuid_ReturnsActualStoredObject()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var subscription = new SubscriptionGroup
        {
            Guid = Guid.NewGuid(),
            WebSocketUrls = new List<string> { "wss://test.com/stream1" },
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            CancellationTokenSource = cancellationTokenSource
        };

        subscriptionGroupManager.TryAdd(subscription);

        // Act
        var retrieved = subscriptionGroupManager.Get(subscription.Guid);

        // Assert
        retrieved.Should().BeSameAs(subscription);
        retrieved.CancellationTokenSource.Should().BeSameAs(cancellationTokenSource);
    }
}
