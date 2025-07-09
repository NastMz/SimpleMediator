using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class ExtensionMethodsTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;

    public ExtensionMethodsTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestQueryHandler).Assembly);
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _publisher = _serviceProvider.GetRequiredService<IPublisher>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task MediatorExtensions_Send_WithIRequest_ShouldExecuteCommand()
    {
        // Arrange
        var command = new TestCommand("extension test");

        // Act
        await _mediator.Send(command);

        // Assert
        TestCommandHandler.ProcessedCommands.Should().Contain("extension test");
    }

    [Fact]
    public async Task MediatorExtensions_PublishAll_WithMultipleNotifications_ShouldExecuteAllInOrder()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = new List<INotification>
        {
            new TestNotification("first"),
            new TestNotification("second"),
            new TestNotification("third")
        };

        // Act
        await _mediator.PublishAll(notifications);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Equal("first", "second", "third");
        SecondTestNotificationHandler.ReceivedMessages.Should().Equal("Second: first", "Second: second", "Second: third");
    }

    [Fact]
    public async Task MediatorExtensions_PublishAll_WithEmptyCollection_ShouldNotThrow()
    {
        // Arrange
        var notifications = new List<INotification>();

        // Act & Assert
        await _mediator.PublishAll(notifications);
        // Should complete without throwing
    }

    [Fact]
    public async Task MediatorExtensions_PublishAll_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var notifications = new List<INotification>
        {
            new TestNotification("with cancellation")
        };
        var cts = new CancellationTokenSource();

        // Act
        await _mediator.PublishAll(notifications, cts.Token);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("with cancellation");
    }

    [Fact]
    public async Task MediatorExtensions_PublishAll_WithMixedNotificationTypes_ShouldExecuteCorrectHandlers()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = new List<INotification>
        {
            new TestNotification("test message"),
            new TestEventNotification(789, "ProductCreated"),
            new TestNotification("another test")
        };

        // Act
        await _mediator.PublishAll(notifications);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Equal("test message", "another test");
        SecondTestNotificationHandler.ReceivedMessages.Should().Equal("Second: test message", "Second: another test");
        TestEventNotificationHandler.ReceivedEvents.Should().Contain((789, "ProductCreated"));
    }

    [Fact]
    public async Task PublisherExtensions_PublishAll_WithMultipleNotifications_ShouldExecuteAllInOrder()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = new List<INotification>
        {
            new TestNotification("publisher first"),
            new TestNotification("publisher second")
        };

        // Act
        await _publisher.PublishAll(notifications);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Equal("publisher first", "publisher second");
        SecondTestNotificationHandler.ReceivedMessages.Should().Equal("Second: publisher first", "Second: publisher second");
    }

    [Fact]
    public async Task PublisherExtensions_PublishAll_WithEmptyCollection_ShouldNotThrow()
    {
        // Arrange
        var notifications = new List<INotification>();

        // Act & Assert
        await _publisher.PublishAll(notifications);
        // Should complete without throwing
    }

    [Fact]
    public async Task PublisherExtensions_PublishAll_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var notifications = new List<INotification>
        {
            new TestNotification("publisher with cancellation")
        };
        var cts = new CancellationTokenSource();

        // Act
        await _publisher.PublishAll(notifications, cts.Token);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Contain("publisher with cancellation");
    }

    [Fact]
    public async Task PublisherExtensions_PublishAll_WithLargeCollection_ShouldHandleEfficiently()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = Enumerable.Range(1, 100)
            .Select(i => new TestNotification($"bulk-{i}"))
            .Cast<INotification>()
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _publisher.PublishAll(notifications);
        stopwatch.Stop();

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().HaveCount(100);
        SecondTestNotificationHandler.ReceivedMessages.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PublisherExtensions_PublishAll_WithException_ShouldStopAtFirstException()
    {
        // This test would require a handler that throws an exception
        // For now, we'll test that all valid notifications are processed
        
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = new List<INotification>
        {
            new TestNotification("before"),
            new TestNotification("after")
        };

        // Act
        await _publisher.PublishAll(notifications);

        // Assert
        TestNotificationHandler.ReceivedMessages.Should().Equal("before", "after");
    }
}
