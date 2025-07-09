using Microsoft.Extensions.DependencyInjection;
using Nast.SimpleMediator.Abstractions;
using System.Reflection;

namespace Nast.SimpleMediator
{
    /// <summary>
    /// Extensions for registering the mediator in the DI container.
    /// </summary>
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Adds the mediator and its dependencies to the container.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediator(this IServiceCollection services)
        {
            return services.AddMediator(AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <summary>
        /// Adds the mediator and its dependencies to the container from the specified assemblies.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to search for handlers</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediator(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddMediator(options => options.RegisterServicesFromAssemblies(assemblies));
        }

        /// <summary>
        /// Adds the mediator and its dependencies to the container with custom options.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediator(
            this IServiceCollection services,
            Action<MediatorOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            var options = new MediatorOptions();
            configureOptions(options);

            services.AddScoped<ISender, Sender>(sp =>
            {
                return new Sender(
                    sp.GetService,
                    t => sp.GetServices(t)
                );
            });

            services.AddScoped<IPublisher, Publisher>(sp =>
            {
                return new Publisher(
                    t => sp.GetServices(t)
                );
            });

            services.AddScoped<IMediator, Mediator>(sp =>
            {
                return new Mediator(
                    sp.GetService,
                    t => sp.GetServices(t)
                );
            });

            // Register request and notification handlers from the specified assemblies
            foreach (var assembly in options.Assemblies)
            {
                RegisterHandlersFromAssembly(services, assembly);
            }

            // Register behaviors
            foreach (var behavior in options.Behaviors)
            {
                services.AddScoped(behavior.InterfaceType, behavior.ImplementationType);
            }

            return services;
        }

        /// <summary>
        /// Registers request and notification handlers from the specified assembly.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly to scan for handlers</param>
        private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
        {
            // Register request handlers
            var requestHandlerType = typeof(IRequestHandler<,>);
            var requestHandlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestHandlerType)
                        .ToList()
                })
                .Where(t => t.Interfaces.Count != 0);

            foreach (var handler in requestHandlerTypes)
            {
                foreach (var handlerInterface in handler.Interfaces)
                {
                    services.AddScoped(handlerInterface, handler.Type);
                }
            }

            // Register notification handlers
            var notificationHandlerType = typeof(INotificationHandler<>);
            var notificationHandlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == notificationHandlerType)
                        .ToList()
                })
                .Where(t => t.Interfaces.Count != 0);

            foreach (var handler in notificationHandlerTypes)
            {
                foreach (var handlerInterface in handler.Interfaces)
                {
                    services.AddScoped(handlerInterface, handler.Type);
                }
            }

            // Register stream request handlers
            var streamRequestHandlerType = typeof(IStreamRequestHandler<,>);
            var streamHandlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == streamRequestHandlerType)
                        .ToList()
                })
                .Where(t => t.Interfaces.Any());

            foreach (var handler in streamHandlerTypes)
            {
                foreach (var handlerInterface in handler.Interfaces)
                {
                    services.AddScoped(handlerInterface, handler.Type);
                }
            }
        }
    }

    /// <summary>
    /// Options for configuring the mediator.
    /// </summary>
    public class MediatorOptions
    {
        /// <summary>
        /// List of assemblies to scan for handlers.
        /// </summary>
        internal List<Assembly> Assemblies { get; } = [];

        /// <summary>
        /// List of behaviors to register.
        /// </summary>
        internal List<BehaviorRegistration> Behaviors { get; } = [];

        /// <summary>
        /// Registers services from the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for handlers</param>
        /// <returns>This instance for chaining</returns>
        public MediatorOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            foreach (var assembly in assemblies)
            {
                Assemblies.Add(assembly);
            }

            return this;
        }

        /// <summary>
        /// Adds a behavior to the mediator.
        /// </summary>
        /// <param name="behaviorInterfaceType">The interface type of the behavior</param>
        /// <param name="behaviorImplementationType">The implementation type of the behavior</param>
        /// <returns>This instance for chaining</returns>
        public MediatorOptions AddBehavior(Type behaviorInterfaceType, Type behaviorImplementationType)
        {
            Behaviors.Add(new BehaviorRegistration(behaviorInterfaceType, behaviorImplementationType));
            return this;
        }

        /// <summary>
        /// Adds a behavior to the mediator using generic types.
        /// </summary>
        /// <typeparam name="TInterfaceType">The interface type of the behavior</typeparam>
        /// <typeparam name="TImplementationType">The implementation type of the behavior</typeparam>
        /// <returns>This instance for chaining</returns>
        public MediatorOptions AddBehavior<TInterfaceType, TImplementationType>()
            where TImplementationType : TInterfaceType
        {
            return AddBehavior(typeof(TInterfaceType), typeof(TImplementationType));
        }
    }

    /// <summary>
    /// Represents a registration for a behavior.
    /// </summary>
    internal class BehaviorRegistration
    {
        /// <summary>
        /// The interface type of the behavior.
        /// </summary>
        public Type InterfaceType { get; }

        /// <summary>
        /// The implementation type of the behavior.
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BehaviorRegistration"/> class.
        /// </summary>
        /// <param name="interfaceType">The interface type of the behavior</param>
        /// <param name="implementationType">The implementation type of the behavior</param>
        public BehaviorRegistration(Type interfaceType, Type implementationType)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
        }
    }
}
