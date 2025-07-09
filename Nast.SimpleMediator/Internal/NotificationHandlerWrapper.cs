using Nast.SimpleMediator.Abstractions;

namespace Nast.SimpleMediator.Internal
{
    /// <summary>
    /// Wrapper for notification handlers that allows for the use of pipeline behaviors.
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    internal class NotificationHandlerWrapper<TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// The notification handler to be wrapped.
        /// </summary>
        private readonly INotificationHandler<TNotification> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationHandlerWrapper{TNotification}"/> class.
        /// </summary>
        /// <param name="handler">The notification handler</param>
        public NotificationHandlerWrapper(INotificationHandler<TNotification> handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Handles the notification.
        /// </summary>
        /// <param name="notification">The notification to handle</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task Handle(object notification, CancellationToken cancellationToken)
        {
            return _handler.Handle((TNotification)notification, cancellationToken);
        }
    }
}
