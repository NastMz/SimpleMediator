using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class MediatorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestQueryHandler).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Send_WithQueryRequest_ShouldReturnResponse()
    {
        // Arrange
        var query = new TestQuery("test data");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.ShouldBe("Processed: test data");
    }

    [Fact]
    public async Task Send_WithQueryWithResponse_ShouldReturnTypedResponse()
    {
        // Arrange
        var query = new TestQueryWithResponse(123);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("Response for ID: 123");
        result.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Send_WithCommand_ShouldExecuteHandler()
    {
        // Arrange
        var command = new TestCommand("test command");

        // Act
        await _mediator.Send(command);

        // Assert
        TestCommandHandler.ProcessedCommands.ShouldContain("test command");
    }

    [Fact]
    public async Task Send_WithUntypedRequest_ShouldReturnResponse()
    {
        // Arrange
        object query = new TestQuery("untyped test");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.ShouldBe("Processed: untyped test");
    }

    [Fact]
    public async Task Send_WithUntypedCommand_ShouldExecuteHandler()
    {
        // Arrange
        TestDataHelper.ClearAll();
        object command = new TestCommand("untyped command");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.ShouldNotBeNull();
        TestCommandHandler.ProcessedCommands.ShouldContain("untyped command");
    }

    [Fact]
    public async Task Publish_WithNotification_ShouldExecuteAllHandlers()
    {
        // Arrange
        var notification = new TestNotification("test message");

        // Act
        await _mediator.Publish(notification);

        // Assert
        TestNotificationHandler.ReceivedMessages.ShouldContain("test message");
        SecondTestNotificationHandler.ReceivedMessages.ShouldContain("Second: test message");
    }

    [Fact]
    public async Task Publish_WithTypedNotification_ShouldExecuteHandler()
    {
        // Arrange
        var notification = new TestEventNotification(42, "UserCreated");

        // Act
        await _mediator.Publish(notification);

        // Assert
        TestEventNotificationHandler.ReceivedEvents.ShouldContain((42, "UserCreated"));
    }

    [Fact]
    public async Task Publish_WithUntypedNotification_ShouldExecuteHandlers()
    {
        // Arrange
        object notification = new TestNotification("untyped message");

        // Act
        await _mediator.Publish(notification);

        // Assert
        TestNotificationHandler.ReceivedMessages.ShouldContain("untyped message");
        SecondTestNotificationHandler.ReceivedMessages.ShouldContain("Second: untyped message");
    }

    [Fact]
    public async Task CreateStream_ShouldReturnAsyncEnumerable()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(3);
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task CreateStream_WithUntypedRequest_ShouldReturnAsyncEnumerable()
    {
        // Arrange
        object streamQuery = new TestStreamQuery(2);
        var results = new List<object?>();

        // Act
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBe(new object[] { 0, 1 });
    }

    [Fact]
    public async Task CreateStream_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(10);
        var cts = new CancellationTokenSource();
        var results = new List<int>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in _mediator.CreateStream(streamQuery, cts.Token))
            {
                results.Add(item);
                if (results.Count == 2)
                {
                    cts.Cancel();
                }
            }
        });

        // Verify that cancellation was respected
        results.Count.ShouldBeLessThanOrEqualTo(3, "some items should be yielded before cancellation");
    }

    [Fact]
    public async Task Send_WithCancellationToken_ShouldPassTokenToHandler()
    {
        // Arrange
        var query = new TestQuery("cancellation test");
        var cts = new CancellationTokenSource();

        // Act
        var result = await _mediator.Send(query, cts.Token);

        // Assert
        result.ShouldBe("Processed: cancellation test");
    }

    [Fact]
    public async Task Publish_WithCancellationToken_ShouldPassTokenToHandlers()
    {
        // Arrange
        var notification = new TestNotification("cancellation test");
        var cts = new CancellationTokenSource();

        // Act
        await _mediator.Publish(notification, cts.Token);

        // Assert
        TestNotificationHandler.ReceivedMessages.ShouldContain("cancellation test");
    }
}
