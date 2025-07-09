namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Marks a class as a request expecting a response of type TResponse.
    /// </summary>
    /// <typeparam name="TResponse">Type of the expected response</typeparam>
    public interface IRequest<out TResponse>
    {
    }

    /// <summary>
    /// Marks a class as a request expecting an empty response (Unit).
    /// </summary>
    public interface IRequest : IRequest<Unit>
    {
    }

    /// <summary>
    /// Type that represents an empty/void operation.
    /// </summary>
    public readonly struct Unit
    {
        /// <summary>
        /// Default global instance of Unit.
        /// </summary>
        public static readonly Unit Value = new();
    }
}
