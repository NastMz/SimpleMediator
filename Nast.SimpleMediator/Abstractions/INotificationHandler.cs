namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a handler for a notification.
    /// </summary>
    /// <typeparam name="TNotification">Type of notification to handle</typeparam>
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles a notification.
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
