using Nast.SimpleMediator.Abstractions;

namespace Nast.SimpleMediator.Internal
{
    /// <summary>
    /// Base class for request handlers.
    /// </summary>
    internal abstract class RequestHandlerBase
    {
        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="request">The request to handle</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response of the request</returns>
        public abstract Task<object?> Handle(object request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Wrapper for request handlers that allows for the use of pipeline behaviors.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    /// <typeparam name="TResponse">Type of the response</typeparam>
    internal class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// The request handler to be wrapped.
        /// </summary>
        private readonly IRequestHandler<TRequest, TResponse> _handler;

        /// <summary>
        /// The pipeline behaviors to be applied to the request.
        /// </summary>
        private readonly IEnumerable<IPipelineBehavior<TRequest, TResponse>> _behaviors;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestHandlerWrapper{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="handler">The request handler</param>
        /// <param name="behaviors">The pipeline behaviors</param>
        public RequestHandlerWrapper(
            IRequestHandler<TRequest, TResponse> handler,
            IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
        {
            _handler = handler;
            _behaviors = behaviors;
        }

        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="request">The request to handle</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response of the request</returns>
        public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
        {
            return await HandleCore((TRequest)request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the request using the pipeline behaviors and the request handler.
        /// </summary>
        /// <param name="request">The request to handle</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response of the request</returns>
        private Task<TResponse> HandleCore(TRequest request, CancellationToken cancellationToken)
        {
            Task<TResponse> Handler() => _handler.Handle(request, cancellationToken);

            return _behaviors
                .Reverse()
                .Aggregate(
                    (RequestHandlerDelegate<TResponse>)Handler,
                    (next, pipeline) => () => pipeline.Handle(request, next, cancellationToken))
                ();
        }
    }
}
