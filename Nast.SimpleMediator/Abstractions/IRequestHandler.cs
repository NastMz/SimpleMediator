namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a handler for a request.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    /// <typeparam name="TResponse">Type of the expected response</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles a request.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Response</returns>
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a handler for a request with an empty response.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
        where TRequest : IRequest<Unit>
    {
    }
}
