namespace Nast.SimpleMediator.Abstractions
{
    /// <summary>
    /// Defines a mediator that combines publishing and sending capabilities.
    /// </summary>
    public interface IMediator : ISender, IPublisher
    {
    }
}
