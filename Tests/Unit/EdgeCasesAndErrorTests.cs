using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class EdgeCasesAndErrorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public EdgeCasesAndErrorTests()
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
    public async Task Send_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Send((TestQuery)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Send((object)null!));
    }

    [Fact]
    public async Task Publish_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Publish((TestNotification)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Publish((object)null!));
    }

    [Fact]
    public void CreateStream_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _mediator.CreateStream((TestStreamQuery)null!));
        Assert.Throws<ArgumentNullException>(() => _mediator.CreateStream((object)null!));
    }

    [Fact]
    public async Task Send_WithUnregisteredHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unregisteredRequest = new UnregisteredRequest("test");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(unregisteredRequest));
    }

    [Fact]
    public async Task CreateStream_WithUnregisteredHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unregisteredStreamRequest = new UnregisteredStreamRequest(5);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
        {
            await foreach (var item in _mediator.CreateStream(unregisteredStreamRequest))
            {
                // Should throw before getting here
                break;
            }
        });
    }

    [Fact]
    public async Task Publish_WithNoHandlers_ShouldCompleteSuccessfully()
    {
        // Arrange
        var notificationWithNoHandlers = new NoHandlersNotification("test");

        // Act & Assert
        await _mediator.Publish(notificationWithNoHandlers); // Should not throw
    }

    [Fact]
    public async Task Send_WithEmptyStringData_ShouldWork()
    {
        // Arrange
        var queryWithEmptyString = new TestQuery("");

        // Act
        var result = await _mediator.Send(queryWithEmptyString);

        // Assert
        result.ShouldBe("Processed: ");
    }

    [Fact]
    public async Task Send_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var queryWithSpecialChars = new TestQuery("Test with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?");

        // Act
        var result = await _mediator.Send(queryWithSpecialChars);

        // Assert
        result.ShouldBe("Processed: Test with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?");
    }

    [Fact]
    public async Task Send_WithVeryLongString_ShouldWork()
    {
        // Arrange
        var longString = new string('A', 10000);
        var queryWithLongString = new TestQuery(longString);

        // Act
        var result = await _mediator.Send(queryWithLongString);

        // Assert
        result.ShouldBe($"Processed: {longString}");
    }

    [Fact]
    public async Task CreateStream_WithZeroCount_ShouldReturnEmptyStream()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(0);
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateStream_WithCancellationAfterCompletion_ShouldComplete()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(3);
        var cts = new CancellationTokenSource();
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(streamQuery, cts.Token))
        {
            results.Add(item);
        }
        
        // Cancel after completion
        cts.Cancel();

        // Assert
        results.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task PublishAll_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.PublishAll((IEnumerable<INotification>)null!));
    }

    [Fact]
    public async Task PublishAll_WithCollectionContainingNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var notifications = new List<INotification>
        {
            new TestNotification("valid"),
            null!,
            new TestNotification("also valid")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.PublishAll(notifications));
    }

    [Fact]
    public void ServiceRegistration_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationExtensions.AddMediator(null!));
    }

    [Fact]
    public void ServiceRegistration_WithNullAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMediator(assemblies: null!));
    }

    [Fact]
    public void ServiceRegistration_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMediator((Action<MediatorOptions>)null!));
    }

    [Fact]
    public async Task ConcurrentAccess_ToStaticCollections_ShouldNotCauseDataRace()
    {
        // Arrange
        TestCommandHandler.ProcessedCommands.Clear();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await _mediator.Send(new TestCommand($"concurrent-{index}"));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Check that our specific concurrent commands were processed
        var concurrentCommands = TestCommandHandler.ProcessedCommands
            .ToArray() // Convert ConcurrentQueue to array first
            .Where(cmd => cmd.StartsWith("concurrent-"))
            .ToList();
        
        concurrentCommands.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            concurrentCommands.ShouldContain($"concurrent-{i}");
        }
    }

    [Fact]
    public async Task DisposedServiceProvider_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestQueryHandler).Assembly);
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        serviceProvider.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await mediator.Send(new TestQuery("test"));
        });
    }

    // Helper types for testing error scenarios
    private record UnregisteredRequest(string Data) : IRequest<string>;
    private record UnregisteredStreamRequest(int Count) : IStreamRequest<int>;
    private record NoHandlersNotification(string Message) : INotification;
}
