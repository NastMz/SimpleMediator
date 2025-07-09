using Nast.SimpleMediator.Abstractions;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Implementation of the IMediator interface.
    /// </summary>
    internal sealed class Mediator : IMediator
    {
        private readonly ISender _sender;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory to create single instances of handlers.</param>
        /// <param name="multiInstanceFactory">The factory to create multiple instances of handlers.</param>
        public Mediator(Func<Type, object?> singleInstanceFactory, Func<Type, IEnumerable<object>> multiInstanceFactory)
        {
            _sender = new Sender(singleInstanceFactory, multiInstanceFactory);
            _publisher = new Publisher(multiInstanceFactory);
        }

        /// <inheritdoc />
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => _sender.Send(request, cancellationToken);

        /// <inheritdoc />
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => _sender.Send(request, cancellationToken);

        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => _sender.CreateStream(request, cancellationToken);

        /// <inheritdoc />
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => _sender.CreateStream(request, cancellationToken);

        /// <inheritdoc />
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => _publisher.Publish(notification, cancellationToken);

        /// <inheritdoc />
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => _publisher.Publish(notification, cancellationToken);
    }
}
