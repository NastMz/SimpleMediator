using Nast.SimpleMediator.Abstractions;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Extensiones para el mediador
    /// </summary>
    public static class MediatorExtensions
    {
        /// <summary>
        /// Sends a request to the mediator for processing that expects a void response.
        /// </summary>
        /// <param name="mediator">The mediator instance used to send the request.</param>
        /// <param name="request">The request to be processed. Cannot be null.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task Send(
            this IMediator mediator,
            IRequest request,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(request, cancellationToken);
        }

        /// <summary>
        /// Publishes a collection of notifications asynchronously using the specified mediator.
        /// </summary>
        /// <param name="mediator">The mediator used to publish the notifications. Cannot be null.</param>
        /// <param name="notifications">A collection of notifications to be published. Cannot be null.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task PublishAll(
            this IMediator mediator,
            IEnumerable<INotification> notifications,
            CancellationToken cancellationToken = default)
        {
            foreach (var notification in notifications)
            {
                await mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
