using Nast.SimpleMediator.Abstractions;
using Nast.SimpleMediator.Internal;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Mediator publisher implementation.
    /// </summary>
    internal sealed class Publisher : IPublisher
    {
        /// <summary>
        /// The factory to create instances of notification handlers.
        /// </summary>
        private readonly Func<Type, IEnumerable<object>> _multiInstanceFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Publisher"/> class.
        /// </summary>
        /// <param name="multiInstanceFactory">The factory to create instances of notification handlers.</param>
        public Publisher(Func<Type, IEnumerable<object>> multiInstanceFactory)
        {
            _multiInstanceFactory = multiInstanceFactory;
        }

        /// <inheritdoc />
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Publish((object)notification, cancellationToken);
        }

        /// <inheritdoc />
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = _multiInstanceFactory(handlerType);

            var handlerTasks = new List<Task>();

            foreach (var handler in handlers)
            {
                var wrapperType = typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType);
                var wrapper = (dynamic)Activator.CreateInstance(wrapperType, handler)!;
                var task = wrapper.Handle((dynamic)notification, cancellationToken);
                handlerTasks.Add(task);
            }

            return Task.WhenAll(handlerTasks);
        }
    }
}
