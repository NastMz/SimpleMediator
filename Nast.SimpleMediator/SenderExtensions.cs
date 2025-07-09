using Nast.SimpleMediator.Abstractions;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Extensions for the sender
    /// </summary>
    public static class SenderExtensions
    {
        /// <summary>
        /// Sends a request with an empty response
        /// </summary>
        /// <param name="sender">The sender instance</param>
        /// <param name="request">The request to send</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task representing the send operation</returns>
        public static Task Send(
            this ISender sender,
            IRequest request,
            CancellationToken cancellationToken = default)
        {
            return sender.Send(request, cancellationToken);
        }
    }
}
