namespace Nast.SimpleMediator.Internal
{
    /// <summary>
    /// Extension methods for ServiceFactoryDelegate to resolve instances of services.
    /// </summary>
    internal static class ServiceFactoryDelegateExtensions
    {
        /// <summary>
        /// Resolves an instance of the specified type using the provided factory delegate.
        /// </summary>
        /// <typeparam name="T">The type of the service to resolve.</typeparam>
        /// <param name="factory">The factory delegate that resolves the service.</param>
        /// <returns>The resolved instance of the specified type.</returns>
        /// <exception cref="InvalidOperationException">If the instance cannot be resolved.</exception>
        public static T GetInstance<T>(this Func<Type, object?> factory)
        {
            var instance = factory(typeof(T)) ?? throw new InvalidOperationException($"Failed to resolve {typeof(T).Name}");
            return (T)instance;
        }

        /// <summary>
        /// Resolves a collection of instances of the specified type using the provided factory delegate.
        /// </summary>
        /// <typeparam name="T">The type of the service to resolve.</typeparam>
        /// <param name="factory">The factory delegate that resolves the service.</param>
        /// <returns>The collection of resolved instances of the specified type.</returns>
        public static IEnumerable<T> GetInstances<T>(this Func<Type, IEnumerable<object>> factory)
        {
            return factory(typeof(T)).Cast<T>();
        }
    }
}
