# SimpleMediator

A simple and efficient implementation of the Mediator pattern for .NET 8 applications, inspired by MediatR but with a minimalist approach and high performance focus.

> **Note**: This library is designed for exploration and learning purposes. While fully functional and well-tested, it is not intended as a commercial solution.

## Features

- ✅ **Mediator Pattern**: Decoupling between components through a central mediator
- ✅ **Request/Response**: Handle commands and queries with typed responses
- ✅ **Notifications**: Event system for one-to-many communication
- ✅ **Stream Requests**: Support for asynchronous data streams
- ✅ **Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection
- ✅ **Auto Registration**: Automatic handler scanning in assemblies
- ✅ **Extensibility**: Support for custom behaviors
- ✅ **.NET 8**: Built for .NET 8 with latest language features

## Installation

```bash
dotnet add package Nast.SimpleMediator
```

## Quick Start

### Basic Setup

```csharp
using Nast.SimpleMediator;
var builder = WebApplication.CreateBuilder(args);

// Register SimpleMediator with automatic assembly scanning builder.Services.AddMediator();
var app = builder.Build();
```

### Advanced Configuration

```csharp
// Register with specific assemblies 
builder.Services.AddMediator(typeof(Program).Assembly, typeof(MyHandlersAssembly).Assembly);

// Configuration with custom options 
builder.Services.AddMediator(options => { options.RegisterServicesFromAssemblies(typeof(Program).Assembly); options.AddBehavior<IMyBehavior, MyBehaviorImplementation>(); });
```

## Usage

### Requests (Commands/Queries)

#### 1. Define a Request

```csharp
// Request with response 
public record GetUserQuery(int UserId) : IRequest<User>;

// Request without response (Command) 
public record CreateUserCommand(string Name, string Email) : IRequest;
```

#### 2. Implement the Handler

```csharp
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    private readonly IUserRepository _repository;

    public GetUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.UserId);
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Name, request.Email);
        await _repository.AddAsync(user);
    }
}
```

#### 3. Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        return await _mediator.Send(new GetUserQuery(id));
    }

    [HttpPost]
    public async Task CreateUser(CreateUserCommand command)
    {
        await _mediator.Send(command);
    }
}
```

### Notifications (Events)

#### 1. Define a Notification

```csharp
public record UserCreatedNotification(int UserId, string Email) : INotification;
```

#### 2. Implement Handlers

```csharp
public class EmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public EmailNotificationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email);
    }
}

public class LoggingNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<LoggingNotificationHandler> _logger;

    public LoggingNotificationHandler(ILogger<LoggingNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} was created", notification.UserId);
    }
}
```

#### 3. Publish Notifications

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;

    public CreateUserHandler(IUserRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Name, request.Email);
        await _repository.AddAsync(user);

        // Publish event - all handlers will be executed
        await _mediator.Publish(new UserCreatedNotification(user.Id, user.Email));
    }
}
```

### Stream Requests (Data Streams)

#### 1. Define a Stream Request

```csharp
public record GetUsersStreamQuery(string Filter) : IStreamRequest<User>;
```

#### 2. Implement the Handler

```csharp
public class GetUsersStreamHandler : IStreamRequestHandler<GetUsersStreamQuery, User>
{
    private readonly IUserRepository _repository;
    public GetUsersStreamHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async IAsyncEnumerable<User> Handle(GetUsersStreamQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var user in _repository.GetFilteredUsersAsync(request.Filter))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return user;
        }
    }
}
```

#### 3. Consume the Stream

```csharp
[HttpGet("stream")]
public async IAsyncEnumerable<User> GetUsersStream(string filter)
{ 
    await foreach (var user in _mediator.CreateStream(new GetUsersStreamQuery(filter))) 
    { 
        yield return user; 
    } 
}
```

## Extension Methods

### Mediator Extensions

```csharp
// Send multiple notifications 
var notifications = new List<INotification> 
{ 
    new UserCreatedNotification(1, "user1@example.com"), 
    new UserCreatedNotification(2, "user2@example.com") 
};

await _mediator.PublishAll(notifications);
```

### Publisher Extensions

```csharp
// Use publisher directly 
await _publisher.PublishAll(notifications);
```

## Core Interfaces

### Main Interfaces

- `IMediator`: Main interface that combines `ISender` and `IPublisher`
- `ISender`: For sending requests and creating streams
- `IPublisher`: For publishing notifications

### Request Interfaces

- `IRequest<TResponse>`: Request that expects a response
- `IRequest`: Request without response (inherits from `IRequest<Unit>`)
- `IStreamRequest<TResponse>`: Request for data streams

### Handler Interfaces

- `IRequestHandler<TRequest, TResponse>`: Handler for requests with response
- `IRequestHandler<TRequest>`: Handler for requests without response
- `INotificationHandler<TNotification>`: Handler for notifications
- `IStreamRequestHandler<TRequest, TResponse>`: Handler for stream requests

## Custom Behaviors

```csharp
public interface ILoggingBehavior 
{ 
    Task ExecuteAsync(Func<Task> next); 
}

public class LoggingBehavior : ILoggingBehavior
{
    private readonly ILogger<LoggingBehavior> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(Func<Task> next)
    {
        _logger.LogInformation("Executing behavior");
        await next();
        _logger.LogInformation("Behavior executed");
    }
}

// Registration 
builder.Services.AddMediator(options => { 
    options.AddBehavior<ILoggingBehavior, LoggingBehavior>(); 
});
```

## Requirements

- .NET 8.0 or higher
- Microsoft.Extensions.DependencyInjection.Abstractions 9.0.7

## Comparison with MediatR

| Feature | SimpleMediator | MediatR |
|---|---|---|
| .NET 8 Native | ✅ | ❌ |
| Size | Lightweight | Full-featured |
| Performance | Optimized | Standard |
| Complexity | Simple | Advanced |
| Stream Support | ✅ | ✅ |
| Behaviors | ✅ | ✅ |

## License

MIT License (MIT) is available at [LICENSE](LICENSE).

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

---

**SimpleMediator** - A simple and efficient alternative to MediatR for .NET 8