using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace Nast.SimpleMediator.Tests.TestData;

// Test requests
public record TestQuery(string Data) : IRequest<string>;

public record TestQueryWithResponse(int Id) : IRequest<TestResponse>;

public record TestCommand(string Name) : IRequest;

public record TestStreamQuery(int Count) : IStreamRequest<int>;

// Test responses
public record TestResponse(string Value, DateTime Timestamp);

// Test notifications
public record TestNotification(string Message) : INotification;

public record TestEventNotification(int EventId, string EventType) : INotification;

// Test handlers
public class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Processed: {request.Data}");
    }
}

public class TestQueryWithResponseHandler : IRequestHandler<TestQueryWithResponse, TestResponse>
{
    public Task<TestResponse> Handle(TestQueryWithResponse request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse($"Response for ID: {request.Id}", DateTime.UtcNow));
    }
}

public class TestCommandHandler : IRequestHandler<TestCommand, Nast.SimpleMediator.Abstractions.Unit>
{
    public static ConcurrentQueue<string> ProcessedCommands { get; } = new();

    public static void Clear() 
    {
        while (ProcessedCommands.TryDequeue(out _)) { }
    }

    public Task<Nast.SimpleMediator.Abstractions.Unit> Handle(TestCommand request, CancellationToken cancellationToken)
    {
        ProcessedCommands.Enqueue(request.Name);
        return Task.FromResult(Nast.SimpleMediator.Abstractions.Unit.Value);
    }
}

public class TestStreamQueryHandler : IStreamRequestHandler<TestStreamQuery, int>
{
    public async IAsyncEnumerable<int> Handle(TestStreamQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Remove artificial delay for performance testing
            yield return i;
        }
        
        // Single async operation to make the method properly async
        await Task.CompletedTask;
    }
}

// Test notification handlers
public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public static ConcurrentQueue<string> ReceivedMessages { get; } = new();

    public static void Clear() 
    {
        while (ReceivedMessages.TryDequeue(out _)) { }
    }

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        ReceivedMessages.Enqueue(notification.Message);
        return Task.CompletedTask;
    }
}

public class SecondTestNotificationHandler : INotificationHandler<TestNotification>
{
    public static ConcurrentQueue<string> ReceivedMessages { get; } = new();

    public static void Clear() 
    {
        while (ReceivedMessages.TryDequeue(out _)) { }
    }

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        ReceivedMessages.Enqueue($"Second: {notification.Message}");
        return Task.CompletedTask;
    }
}

public class TestEventNotificationHandler : INotificationHandler<TestEventNotification>
{
    public static ConcurrentQueue<(int EventId, string EventType)> ReceivedEvents { get; } = new();

    public static void Clear() 
    {
        while (ReceivedEvents.TryDequeue(out _)) { }
    }

    public Task Handle(TestEventNotification notification, CancellationToken cancellationToken)
    {
        ReceivedEvents.Enqueue((notification.EventId, notification.EventType));
        return Task.CompletedTask;
    }
}

// Test behavior
public interface ITestBehavior
{
    Task ExecuteAsync(Func<Task> next);
}

public class TestBehavior : ITestBehavior
{
    public static ConcurrentQueue<string> ExecutionLog { get; } = new();

    public static void Clear() 
    {
        while (ExecutionLog.TryDequeue(out _)) { }
    }

    public async Task ExecuteAsync(Func<Task> next)
    {
        ExecutionLog.Enqueue("Before");
        await next();
        ExecutionLog.Enqueue("After");
    }
}

// Test helper to clear all static collections
public static class TestDataHelper
{
    public static void ClearAll()
    {
        TestCommandHandler.Clear();
        TestNotificationHandler.Clear();
        SecondTestNotificationHandler.Clear();
        TestEventNotificationHandler.Clear();
        TestBehavior.Clear();
    }
}
