using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class SenderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ISender _sender;

    public SenderTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestQueryHandler).Assembly);
        
        _serviceProvider = services.BuildServiceProvider();
        _sender = _serviceProvider.GetRequiredService<ISender>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Send_WithQuery_ShouldReturnResponse()
    {
        // Arrange
        var query = new TestQuery("sender test");

        // Act
        var result = await _sender.Send(query);

        // Assert
        result.ShouldBe("Processed: sender test");
    }

    [Fact]
    public async Task Send_WithCommand_ShouldExecuteHandler()
    {
        // Arrange
        var command = new TestCommand("sender command");

        // Act
        await _sender.Send(command);

        // Assert
        TestCommandHandler.ProcessedCommands.ShouldContain("sender command");
    }

    [Fact]
    public async Task Send_WithUntypedQuery_ShouldReturnResponse()
    {
        // Arrange
        object query = new TestQuery("untyped sender test");

        // Act
        var result = await _sender.Send(query);

        // Assert
        result.ShouldBe("Processed: untyped sender test");
    }

    [Fact]
    public async Task Send_WithUntypedCommand_ShouldExecuteHandler()
    {
        // Arrange
        TestDataHelper.ClearAll();
        object command = new TestCommand("untyped sender command");

        // Act
        var result = await _sender.Send(command);

        // Assert
        result.ShouldNotBeNull();
        TestCommandHandler.ProcessedCommands.ShouldContain("untyped sender command");
    }

    [Fact]
    public async Task CreateStream_WithTypedRequest_ShouldReturnStream()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(3);
        var results = new List<int>();

        // Act
        await foreach (var item in _sender.CreateStream(streamQuery))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task CreateStream_WithUntypedRequest_ShouldReturnStream()
    {
        // Arrange
        object streamQuery = new TestStreamQuery(2);
        var results = new List<object?>();

        // Act
        await foreach (var item in _sender.CreateStream(streamQuery))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBe(new object[] { 0, 1 });
    }

    [Fact]
    public async Task CreateStream_WithCancellation_ShouldStopEarly()
    {
        // Arrange
        var streamQuery = new TestStreamQuery(10);
        var cts = new CancellationTokenSource();
        var results = new List<int>();

        // Act
        try
        {
            await foreach (var item in _sender.CreateStream(streamQuery, cts.Token))
            {
                results.Add(item);
                if (results.Count == 3)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task Send_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var query = new TestQuery("cancellation test");
        var cts = new CancellationTokenSource();

        // Act
        var result = await _sender.Send(query, cts.Token);

        // Assert
        result.ShouldBe("Processed: cancellation test");
    }

    [Fact]
    public async Task Send_WithComplexResponse_ShouldReturnCorrectType()
    {
        // Arrange
        var query = new TestQueryWithResponse(999);

        // Act
        var result = await _sender.Send(query);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("Response for ID: 999");
        result.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Send_NonExistentHandler_ShouldThrowException()
    {
        // Arrange
        var invalidQuery = new InvalidQuery("test");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sender.Send(invalidQuery));
    }

    // Helper record for testing missing handler scenario
    private record InvalidQuery(string Data) : IRequest<string>;
}
