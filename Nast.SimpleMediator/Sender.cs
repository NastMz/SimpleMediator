using Nast.SimpleMediator.Abstractions;
using Nast.SimpleMediator.Internal;
using System.Collections.Concurrent;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Mediator sender implementation.
    /// </summary>
    internal sealed class Sender : ISender
    {
        /// <summary>
        /// Factory to create single instances of handlers.
        /// </summary>
        private readonly Func<Type, object?> _singleInstanceFactory;

        /// <summary>
        /// Factory to create multiple instances of handlers.
        /// </summary>
        private readonly Func<Type, IEnumerable<object>> _multiInstanceFactory;

        /// <summary>
        /// Cache for handler metadata, not handler instances.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HandlerMetadata> _handlersMetadata = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Sender"/> class.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory to create single instances of handlers.</param>
        /// <param name="multiInstanceFactory">The factory to create multiple instances of handlers.</param>
        public Sender(Func<Type, object?> singleInstanceFactory, Func<Type, IEnumerable<object>> multiInstanceFactory)
        {
            _singleInstanceFactory = singleInstanceFactory;
            _multiInstanceFactory = multiInstanceFactory;
        }

        /// <inheritdoc />
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var result = await Send(request, typeof(TResponse), cancellationToken);
            return (TResponse)result;
        }

        /// <inheritdoc />
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestType = request.GetType();
            var responseType = GetResponseType(requestType);

            return Send(request, responseType, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestType = request.GetType();
            var responseType = typeof(TResponse);
            var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = _singleInstanceFactory(handlerType);

            return handler == null
                ? throw new InvalidOperationException($"No stream handler registered for {requestType.Name}")
                : (IAsyncEnumerable<TResponse>)((dynamic)handler).Handle((dynamic)request, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestType = request.GetType();
            var responseType = GetStreamResponseType(requestType);
            var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = _singleInstanceFactory(handlerType);

            return handler == null
                ? throw new InvalidOperationException($"No stream handler registered for {requestType.Name}")
                : (IAsyncEnumerable<object?>)((dynamic)handler).Handle((dynamic)request, cancellationToken).Cast<object?>();
        }

        /// <summary>
        /// Sends a request and returns the response (private method).
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="responseType">Type of the expected response</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response from the handler</returns>
        private Task<object?> Send(object request, Type responseType, CancellationToken cancellationToken)
        {
            var requestType = request.GetType();
            var metadata = GetHandlerMetadata(requestType, responseType);

            // Create a new handler instance for each request
            var handler = CreateHandler(metadata, requestType, responseType);

            return handler.Handle(request, cancellationToken);
        }

        /// <summary>
        /// Gets the handler metadata for the specified request and response types.
        /// </summary>
        /// <param name="requestType">Type of the request</param>
        /// <param name="responseType">Type of the response</param>
        /// <returns>The handler metadata</returns>
        private HandlerMetadata GetHandlerMetadata(Type requestType, Type responseType)
        {
            return _handlersMetadata.GetOrAdd(requestType, capturedRequestType =>
            {
                var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(capturedRequestType, responseType);
                var handlerWrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(capturedRequestType, responseType);
                var behaviorsType = typeof(IPipelineBehavior<,>).MakeGenericType(capturedRequestType, responseType);

                return new HandlerMetadata
                {
                    HandlerInterfaceType = handlerInterfaceType,
                    HandlerWrapperType = handlerWrapperType,
                    BehaviorsType = behaviorsType
                };
            });
        }

        /// <summary>
        /// Creates a handler instance for the specified request and response types.
        /// </summary>
        /// <param name="metadata">The handler metadata</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="responseType">Type of the response</param>
        /// <returns>The request handler</returns>
        /// <exception cref="InvalidOperationException">If no handler is registered for the request type</exception>
        private RequestHandlerBase CreateHandler(HandlerMetadata metadata, Type requestType, Type responseType)
        {
            var handler = _singleInstanceFactory(metadata.HandlerInterfaceType) ??
                throw new InvalidOperationException($"No handler registered for {requestType.Name} with response type {responseType.Name}");

            var behaviors = _multiInstanceFactory(metadata.BehaviorsType);

            return (RequestHandlerBase)Activator.CreateInstance(
                metadata.HandlerWrapperType,
                handler,
                behaviors)!;
        }

        /// <summary>
        /// Gets the response type from the request type.
        /// </summary>
        /// <param name="requestType">Type of the request</param>
        /// <returns>The response type</returns>
        /// <exception cref="InvalidOperationException">If the request type does not implement IRequest<TResponse></exception>
        private static Type GetResponseType(Type requestType)
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            return requestInterface == null
                ? throw new InvalidOperationException($"{requestType.Name} does not implement IRequest<TResponse>")
                : requestInterface.GetGenericArguments()[0];
        }

        /// <summary>
        /// Gets the response type from the stream request type.
        /// </summary>
        /// <param name="requestType">Type of the request</param>
        /// <returns>The response type</returns>
        /// <exception cref="InvalidOperationException">If the request type does not implement IStreamRequest<TResponse></exception>
        private static Type GetStreamResponseType(Type requestType)
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));

            return requestInterface == null
                ? throw new InvalidOperationException($"{requestType.Name} does not implement IStreamRequest<T>")
                : requestInterface.GetGenericArguments()[0];
        }
    }

    /// <summary>
    /// Metadata about handler types, used for caching.
    /// </summary>
    internal class HandlerMetadata
    {
        /// <summary>
        /// Gets or sets the handler interface type.
        /// </summary>
        public Type HandlerInterfaceType { get; set; }

        /// <summary>
        /// Gets or sets the handler wrapper type.
        /// </summary>
        public Type HandlerWrapperType { get; set; }

        /// <summary>
        /// Gets or sets the behaviors type.
        /// </summary>
        public Type BehaviorsType { get; set; }
    }
}
