namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a handler for a stream request.
    /// </summary>
    /// <typeparam name="TRequest">Type of the stream request</typeparam>
    /// <typeparam name="TResponse">Type of the response elements</typeparam>
    public interface IStreamRequestHandler<in TRequest, out TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Handles a stream request.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stream of responses</returns>
        IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
