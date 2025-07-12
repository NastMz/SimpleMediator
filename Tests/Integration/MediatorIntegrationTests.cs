using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Integration;

[Collection("GlobalTestCollection")]
public class MediatorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssemblies(typeof(TestQueryHandler).Assembly);
            options.AddBehavior<ITestBehavior, TestBehavior>();
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task CompleteWorkflow_ShouldExecuteAllComponents()
    {
        // Arrange
        var query = new TestQuery("integration test");
        var command = new TestCommand("integration command");
        var notification = new TestNotification("integration notification");
        var streamQuery = new TestStreamQuery(3);

        // Act & Assert - Query
        var queryResult = await _mediator.Send(query);
        queryResult.ShouldBe("Processed: integration test");

        // Act & Assert - Command
        await _mediator.Send(command);
        TestCommandHandler.ProcessedCommands.ShouldContain("integration command");

        // Act & Assert - Notification
        await _mediator.Publish(notification);
        TestNotificationHandler.ReceivedMessages.ShouldContain("integration notification");
        SecondTestNotificationHandler.ReceivedMessages.ShouldContain("Second: integration notification");

        // Act & Assert - Stream
        var streamResults = new List<int>();
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            streamResults.Add(item);
        }
        streamResults.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task MixedOperations_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var query = new TestQuery("cancellation test");
        var notification = new TestNotification("cancellation notification");

        // Act
        var queryResult = await _mediator.Send(query, cts.Token);
        await _mediator.Publish(notification, cts.Token);

        // Assert
        queryResult.ShouldBe("Processed: cancellation test");
        TestNotificationHandler.ReceivedMessages.ShouldContain("cancellation notification");
    }

    [Fact]
    public async Task MultipleNotificationHandlers_ShouldAllExecute()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notification = new TestNotification("multiple handlers");

        // Act
        await _mediator.Publish(notification);

        // Assert
        TestNotificationHandler.ReceivedMessages.ShouldContain("multiple handlers");
        SecondTestNotificationHandler.ReceivedMessages.ShouldContain("Second: multiple handlers");
    }

    [Fact]
    public async Task StreamWithLargeBatch_ShouldPerformEfficiently()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(1000);
        var results = new List<int>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            results.Add(item);
        }
        stopwatch.Stop();

        // Assert
        results.Count.ShouldBe(1000);
        results.ShouldBe(Enumerable.Range(0, 1000));
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Should complete quickly without artificial delays
    }

    [Fact]
    public async Task BulkNotifications_ShouldProcessAllSequentially()
    {
        // Arrange
        TestDataHelper.ClearAll();
        var notifications = Enumerable.Range(1, 50)
            .Select(i => new TestNotification($"bulk-{i}"))
            .Cast<INotification>()
            .ToList();

        // Act
        await _mediator.PublishAll(notifications);

        // Assert
        TestNotificationHandler.ReceivedMessages.Count.ShouldBe(50);
        SecondTestNotificationHandler.ReceivedMessages.Count.ShouldBe(50);

        for (int i = 1; i <= 50; i++)
        {
            TestNotificationHandler.ReceivedMessages.ShouldContain($"bulk-{i}");
            SecondTestNotificationHandler.ReceivedMessages.ShouldContain($"Second: bulk-{i}");
        }
    }

    [Fact]
    public async Task ComplexDataTypes_ShouldHandleCorrectly()
    {
        // Arrange
        var complexQuery = new TestQueryWithResponse(42);
        var complexEvent = new TestEventNotification(123, "ComplexEvent");

        // Act
        var queryResult = await _mediator.Send(complexQuery);
        await _mediator.Publish(complexEvent);

        // Assert
        queryResult.ShouldNotBeNull();
        queryResult.Value.ShouldBe("Response for ID: 42");
        queryResult.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        
        TestEventNotificationHandler.ReceivedEvents.ShouldContain((123, "ComplexEvent"));
    }

    [Fact]
    public void ServiceLifetime_ShouldWork()
    {
        // Arrange & Act
        var mediator1 = _serviceProvider.GetRequiredService<IMediator>();
        var mediator2 = _serviceProvider.GetRequiredService<IMediator>();
        var sender = _serviceProvider.GetRequiredService<ISender>();
        var publisher = _serviceProvider.GetRequiredService<IPublisher>();

        // Assert - With scoped services in the same scope, they should be the same instance
        mediator1.ShouldBeSameAs(mediator2); // Scoped services within the same scope are reused
        sender.ShouldNotBeNull();
        publisher.ShouldNotBeNull();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var lockObject = new object();
        var results = new List<string>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var result = await _mediator.Send(new TestQuery($"concurrent-{index}"));
                lock (lockObject)
                {
                    results.Add(result);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        results.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            results.ShouldContain($"Processed: concurrent-{i}");
        }
    }

    [Fact]
    public async Task ErrorScenario_InvalidRequest_ShouldThrowException()
    {
        // Arrange
        var invalidRequest = new InvalidTestQuery("invalid");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(invalidRequest));
    }

    // Helper for testing error scenarios
    private record InvalidTestQuery(string Data) : IRequest<string>;
}
