namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a sender to encapsulate requests expecting responses.
    /// </summary>
    public interface ISender
    {
        /// <summary>
        /// Sends a request to the single handler.
        /// </summary>
        /// <typeparam name="TResponse">Type of the expected response</typeparam>
        /// <param name="request">Request to send</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Response from the handler</returns>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request to the single handler (untyped version).
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Response from the handler, if any</returns>
        Task<object?> Send(object request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a stream for a stream request.
        /// </summary>
        /// <typeparam name="TResponse">Type of the stream elements</typeparam>
        /// <param name="request">Stream request</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stream of responses</returns>
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a stream for a stream request (untyped version).
        /// </summary>
        /// <param name="request">Stream request</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stream of responses</returns>
        IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default);
    }
}
