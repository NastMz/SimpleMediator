namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Behavior in the mediation pipeline to implement aspects such as logging, validation, etc.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    /// <typeparam name="TResponse">Type of the response</typeparam>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : notnull
    {
        /// <summary>
        /// Pipeline handler for the current request.
        /// </summary>
        /// <param name="request">Incoming request</param>
        /// <param name="next">Delegate for the next handler in the pipeline</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Response from the pipeline</returns>
        Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Delegate representing the next handler in the pipeline.
    /// </summary>
    /// <typeparam name="TResponse">Type of the expected response</typeparam>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
}
