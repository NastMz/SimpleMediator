using System.Diagnostics;
using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Performance;

[Collection("GlobalTestCollection")]
public class PerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public PerformanceTests()
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
    public async Task Send_Performance_ShouldBeReasonablyFast()
    {
        // Arrange
        const int iterations = 1000;
        var queries = Enumerable.Range(0, iterations)
            .Select(i => new TestQuery($"perf-test-{i}"))
            .ToArray();

        // Warmup
        await _mediator.Send(new TestQuery("warmup"));

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var query in queries)
        {
            await _mediator.Send(query);
        }
        
        stopwatch.Stop();

        // Assert
        var avgTimePerRequest = (double)stopwatch.ElapsedMilliseconds / iterations;
        avgTimePerRequest.ShouldBeLessThan(1.0); // Less than 1ms per request on average
        
        // Output for diagnostics
        Console.WriteLine($"Average time per Send: {avgTimePerRequest:F3}ms");
        Console.WriteLine($"Total time for {iterations} requests: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Publish_Performance_ShouldBeReasonablyFast()
    {
        // Arrange
        TestDataHelper.ClearAll();
        const int iterations = 500;
        var notifications = Enumerable.Range(0, iterations)
            .Select(i => new TestNotification($"perf-notify-{i}"))
            .ToArray();

        // Warmup
        await _mediator.Publish(new TestNotification("warmup"));

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var notification in notifications)
        {
            await _mediator.Publish(notification);
        }
        
        stopwatch.Stop();

        // Assert
        var avgTimePerPublish = (double)stopwatch.ElapsedMilliseconds / iterations;
        avgTimePerPublish.ShouldBeLessThan(2.0); // Less than 2ms per publish (has 2 handlers)
        
        TestNotificationHandler.ReceivedMessages.Count.ShouldBe(iterations + 1); // +1 for warmup
        SecondTestNotificationHandler.ReceivedMessages.Count.ShouldBe(iterations + 1);
        
        // Output for diagnostics
        Console.WriteLine($"Average time per Publish: {avgTimePerPublish:F3}ms");
        Console.WriteLine($"Total time for {iterations} notifications: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Stream_Performance_ShouldHandleLargeVolumes()
    {
        // Arrange
        const int streamSize = 1000; // Reduced from 10000 for more realistic CI/CD expectations
        var streamQuery = new TestStreamQuery(streamSize);
        var results = new List<int>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        await foreach (var item in _mediator.CreateStream(streamQuery))
        {
            results.Add(item);
        }
        
        stopwatch.Stop();

        // Assert
        results.Count.ShouldBe(streamSize);
        results.ShouldBe(Enumerable.Range(0, streamSize));
        
        var avgTimePerItem = (double)stopwatch.ElapsedMilliseconds / streamSize;
        avgTimePerItem.ShouldBeLessThan(0.1); // Without artificial delays, should be very fast
        
        // Output for diagnostics
        Console.WriteLine($"Average time per stream item: {avgTimePerItem:F4}ms");
        Console.WriteLine($"Total time for {streamSize} items: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Items per second: {streamSize / (stopwatch.ElapsedMilliseconds / 1000.0):F0}");
    }

    [Fact]
    public async Task ConcurrentSend_Performance_ShouldScaleWell()
    {
        // Arrange
        const int concurrentRequests = 100;
        const int requestsPerTask = 10;
        
        // Warmup
        await _mediator.Send(new TestQuery("warmup"));

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async taskId =>
            {
                for (int i = 0; i < requestsPerTask; i++)
                {
                    await _mediator.Send(new TestQuery($"concurrent-{taskId}-{i}"));
                }
            });
            
        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        // Assert
        var totalRequests = concurrentRequests * requestsPerTask;
        var avgTimePerRequest = (double)stopwatch.ElapsedMilliseconds / totalRequests;
        avgTimePerRequest.ShouldBeLessThan(5.0); // Should handle concurrency well
        
        // Output for diagnostics
        Console.WriteLine($"Total concurrent requests: {totalRequests}");
        Console.WriteLine($"Average time per concurrent request: {avgTimePerRequest:F3}ms");
        Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Requests per second: {totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0):F0}");
    }

    [Fact]
    public async Task BulkPublish_Performance_ShouldBeFast()
    {
        // Arrange
        TestDataHelper.ClearAll();
        const int bulkSize = 1000;
        var notifications = Enumerable.Range(0, bulkSize)
            .Select(i => new TestNotification($"bulk-{i}"))
            .Cast<INotification>()
            .ToList();

        // Warmup
        await _mediator.PublishAll(new[] { new TestNotification("warmup") });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _mediator.PublishAll(notifications);
        stopwatch.Stop();

        // Assert
        var avgTimePerNotification = (double)stopwatch.ElapsedMilliseconds / bulkSize;
        avgTimePerNotification.ShouldBeLessThan(2.0); // Should be efficient for bulk operations
        
        // Count only our bulk notifications (not any contamination from other tests)
        var allMessages = TestNotificationHandler.ReceivedMessages.ToArray();
        var allSecondMessages = SecondTestNotificationHandler.ReceivedMessages.ToArray();
        
        var bulkNotifications = allMessages
            .Where(msg => msg.StartsWith("bulk-") || msg == "warmup")
            .ToList();
        var secondBulkNotifications = allSecondMessages
            .Where(msg => msg.StartsWith("Second: bulk-") || msg == "Second: warmup")
            .ToList();
        
        // Debug output
        Console.WriteLine($"All messages count: {allMessages.Length}");
        Console.WriteLine($"Bulk messages count: {bulkNotifications.Count}");
        Console.WriteLine($"All second messages count: {allSecondMessages.Length}");
        Console.WriteLine($"Second bulk messages count: {secondBulkNotifications.Count}");
        if (allMessages.Length > 0)
        {
            Console.WriteLine($"First 5 messages: {string.Join(", ", allMessages.Take(5))}");
        }
        
        bulkNotifications.Count.ShouldBe(bulkSize + 1); // +1 for warmup
        secondBulkNotifications.Count.ShouldBe(bulkSize + 1);
        
        // Output for diagnostics
        Console.WriteLine($"Bulk publish of {bulkSize} notifications: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average time per bulk notification: {avgTimePerNotification:F3}ms");
    }

    [Fact]
    public async Task MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        const int iterations = 1000;
        
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _mediator.Send(new TestQuery($"memory-test-{i}"));
            await _mediator.Publish(new TestNotification($"memory-notify-{i}"));
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        var memoryPerOperation = memoryUsed / (iterations * 2); // Send + Publish per iteration
        memoryPerOperation.ShouldBeLessThan(1024); // Less than 1KB per operation
        
        // Output for diagnostics
        Console.WriteLine($"Memory used for {iterations * 2} operations: {memoryUsed} bytes");
        Console.WriteLine($"Average memory per operation: {memoryPerOperation} bytes");
    }

    [Fact]
    public void ServiceResolution_Performance_ShouldBeFast()
    {
        // Arrange
        const int iterations = 1000;

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var sender = _serviceProvider.GetRequiredService<ISender>();
            var publisher = _serviceProvider.GetRequiredService<IPublisher>();
            
            // Use the services to ensure they're properly resolved
            _ = mediator.GetType();
            _ = sender.GetType();
            _ = publisher.GetType();
        }
        
        stopwatch.Stop();

        // Assert
        var avgResolutionTime = (double)stopwatch.ElapsedMilliseconds / iterations;
        avgResolutionTime.ShouldBeLessThan(0.1); // Service resolution should be very fast
        
        // Output for diagnostics
        Console.WriteLine($"Average service resolution time: {avgResolutionTime:F4}ms");
        Console.WriteLine($"Total time for {iterations} resolutions: {stopwatch.ElapsedMilliseconds}ms");
    }
}
