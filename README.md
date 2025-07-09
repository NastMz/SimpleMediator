# SimpleMediator

A lightweight and efficient implementation of the Mediator pattern for .NET 8 applications. SimpleMediator provides a clean abstraction for handling commands, queries, and notifications while maintaining simplicity and performance.

> **Note**: This library is designed as an educational implementation and is not intended as a commercial solution.

## Features

- **Request/Response Pattern**: Handle commands and queries with typed responses
- **Notifications**: Event-driven architecture with one-to-many communication
- **Stream Requests**: Support for asynchronous data streams using `IAsyncEnumerable`
- **Dependency Injection**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Auto Registration**: Automatic discovery and registration of handlers from assemblies
- **Custom Behaviors**: Extensible pipeline for cross-cutting concerns
- **.NET 8 Native**: Built specifically for .NET 8 with modern C# features

## Installation

Add the package reference to your project:

```bash
dotnet add package Nast.SimpleMediator
```

## Quick Start

Register SimpleMediator in your dependency injection container:

```csharp
using Nast.SimpleMediator;

var builder = WebApplication.CreateBuilder(args);

// Basic registration - scans all loaded assemblies
builder.Services.AddMediator();

// Register specific assemblies
builder.Services.AddMediator(typeof(Program).Assembly);

// Advanced configuration
builder.Services.AddMediator(options =>
{
    options.RegisterServicesFromAssemblies(typeof(Program).Assembly);
    options.AddBehavior<ILoggingBehavior, LoggingBehavior>();
});

var app = builder.Build();
```

## Usage

### Requests and Responses

SimpleMediator supports both commands (requests without responses) and queries (requests with responses).

#### Define a Request

```csharp
// Query with response
public record GetUserQuery(int UserId) : IRequest<User>;

// Command without response
public record CreateUserCommand(string Name, string Email) : IRequest;
```

#### Implement Request Handlers

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

#### Use in Controllers

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

### Notifications

Notifications allow decoupled event-driven communication where multiple handlers can respond to a single event.

#### Define a Notification

```csharp
public record UserCreatedNotification(int UserId, string Email) : INotification;
```

#### Implement Notification Handlers

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

#### Publish Notifications

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

        // Publish notification - all handlers will be executed
        await _mediator.Publish(new UserCreatedNotification(user.Id, user.Email));
    }
}
```

### Stream Requests

Stream requests enable asynchronous data streaming using `IAsyncEnumerable<T>`.

#### Define a Stream Request

```csharp
public record GetUsersStreamQuery(string Filter) : IStreamRequest<User>;
```

#### Implement Stream Handler

```csharp
public class GetUsersStreamHandler : IStreamRequestHandler<GetUsersStreamQuery, User>
{
    private readonly IUserRepository _repository;

    public GetUsersStreamHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async IAsyncEnumerable<User> Handle(
        GetUsersStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var user in _repository.GetFilteredUsersAsync(request.Filter))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return user;
        }
    }
}
```

#### Consume the Stream

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

SimpleMediator provides several convenience extension methods:

```csharp
// Send multiple notifications
var notifications = new List<INotification>
{
    new UserCreatedNotification(1, "user1@example.com"),
    new UserCreatedNotification(2, "user2@example.com")
};

await _mediator.PublishAll(notifications);

// Use publisher directly
await _publisher.PublishAll(notifications);
```

## Core Interfaces

### Main Abstractions

- **`IMediator`**: Primary interface combining `ISender` and `IPublisher`
- **`ISender`**: Handles request/response operations and stream creation
- **`IPublisher`**: Manages notification publishing

### Request Abstractions

- **`IRequest<TResponse>`**: Request expecting a typed response
- **`IRequest`**: Request without response (command pattern)
- **`IStreamRequest<TResponse>`**: Request for streaming data

### Handler Abstractions

- **`IRequestHandler<TRequest, TResponse>`**: Handles requests with responses
- **`IRequestHandler<TRequest>`**: Handles requests without responses
- **`INotificationHandler<TNotification>`**: Handles notifications
- **`IStreamRequestHandler<TRequest, TResponse>`**: Handles stream requests

## Custom Behaviors

You can extend SimpleMediator with custom behaviors for cross-cutting concerns:

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
        _logger.LogInformation("Executing operation");
        await next();
        _logger.LogInformation("Operation completed");
    }
}

// Register behavior
builder.Services.AddMediator(options =>
{
    options.AddBehavior<ILoggingBehavior, LoggingBehavior>();
});
```

## Requirements

- **.NET 8.0** or higher
- **Microsoft.Extensions.DependencyInjection.Abstractions** 9.0.7

## Architecture

SimpleMediator follows a clean architecture approach with clear separation of concerns:

- **Abstractions**: Core interfaces defining contracts
- **Internal**: Implementation details hidden from consumers
- **Extensions**: Convenience methods for common operations
- **Registration**: Dependency injection configuration

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
