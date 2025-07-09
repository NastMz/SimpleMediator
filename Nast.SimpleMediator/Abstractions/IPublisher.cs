namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a publisher to send notifications.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <param name="notification">Notification to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task Publish(object notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">Type of the notification</typeparam>
        /// <param name="notification">Notification to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
