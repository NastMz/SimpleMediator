using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class PublisherTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IPublisher _publisher;

    public PublisherTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestQueryHandler).Assembly);
        
        _serviceProvider = services.BuildServiceProvider();
        _publisher = _serviceProvider.GetRequiredService<IPublisher>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Publish_WithTypedNotification_ShouldExecuteAllHandlers()
    {
        // Arrange
        var notification = new TestNotification("publisher test");

        // Act
        await _publisher.Publish(notification);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("publisher test");
        SecondTestNotificationHandler.ReceivedMessages.Should().Contain("Second: publisher test");
    }

    [Fact]
    public async Task Publish_WithUntypedNotification_ShouldExecuteAllHandlers()
    {
        // Arrange
        object notification = new TestNotification("untyped publisher test");

        // Act
        await _publisher.Publish(notification);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("untyped publisher test");
        SecondTestNotificationHandler.ReceivedMessages.Should().Contain("Second: untyped publisher test");
    }

    [Fact]
    public async Task Publish_WithEventNotification_ShouldExecuteSpecificHandler()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notification = new TestEventNotification(123, "OrderPlaced");

        // Act
        await _publisher.Publish(notification);

        // Assert
        TestEventNotificationHandler.ReceivedEvents.Should().Contain((123, "OrderPlaced"));
        TestNotificationHandler.ReceivedMessages.Should().BeEmpty();
        SecondTestNotificationHandler.ReceivedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_WithMultipleNotifications_ShouldExecuteInOrder()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notification1 = new TestNotification("first");
        var notification2 = new TestNotification("second");

        // Act
        await _publisher.Publish(notification1);
        await _publisher.Publish(notification2);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Equal("first", "second");
        SecondTestNotificationHandler.ReceivedMessages.Should().Equal("Second: first", "Second: second");
    }

    [Fact]
    public async Task Publish_WithCancellationToken_ShouldPassTokenToHandlers()
    {
        // Arrange
        var notification = new TestNotification("cancellation test");
        var cts = new CancellationTokenSource();

        // Act
        await _publisher.Publish(notification, cts.Token);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("cancellation test");
    }

    [Fact]
    public async Task Publish_WithNoHandlers_ShouldNotThrow()
    {
        // Arrange
        var notification = new NoHandlerNotification("no handler");

        // Act & Assert
        await _publisher.Publish(notification);
        // Should complete without throwing
    }

    [Fact]
    public async Task Publish_WithMixedNotificationTypes_ShouldExecuteCorrectHandlers()
    {
        // Arrange
        var testNotification = new TestNotification("test message");
        var eventNotification = new TestEventNotification(456, "UserDeleted");

        // Act
        await _publisher.Publish(testNotification);
        await _publisher.Publish(eventNotification);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("test message");
        SecondTestNotificationHandler.ReceivedMessages.Should().Contain("Second: test message");
        TestEventNotificationHandler.ReceivedEvents.Should().Contain((456, "UserDeleted"));
    }

    [Fact]
    public async Task Publish_ConcurrentNotifications_ShouldHandleCorrectly()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = Enumerable.Range(1, 10)
            .Select(i => new TestNotification($"message-{i}"))
            .ToArray();

        // Act
        var tasks = notifications.Select(n => _publisher.Publish(n));
        await Task.WhenAll(tasks);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().HaveCount(10);
        SecondTestNotificationHandler.ReceivedMessages.Should().HaveCount(10);
        
        foreach (var i in Enumerable.Range(1, 10))
        {
            TestNotificationHandler.ReceivedMessages.Should().Contain($"message-{i}");
            SecondTestNotificationHandler.ReceivedMessages.Should().Contain($"Second: message-{i}");
        }
    }

    // Helper record for testing no handler scenario
    private record NoHandlerNotification(string Message) : INotification;
}
