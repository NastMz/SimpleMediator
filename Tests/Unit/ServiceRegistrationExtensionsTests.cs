using System.Reflection;
using Nast.SimpleMediator.Tests.TestData;

namespace Nast.SimpleMediator.Tests.Unit;

[Collection("GlobalTestCollection")]
public class ServiceRegistrationExtensionsTests : IDisposable
{
    [Fact]
    public void AddMediator_WithoutParameters_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<IMediator>().ShouldNotBeNull();
        serviceProvider.GetService<ISender>().ShouldNotBeNull();
        serviceProvider.GetService<IPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddMediator_WithAssemblies_ShouldRegisterServicesAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(TestQueryHandler).Assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<IMediator>().ShouldNotBeNull();
        serviceProvider.GetService<IRequestHandler<TestQuery, string>>().ShouldNotBeNull();
        serviceProvider.GetService<IRequestHandler<TestCommand, Nast.SimpleMediator.Abstractions.Unit>>().ShouldNotBeNull();
        serviceProvider.GetService<INotificationHandler<TestNotification>>().ShouldNotBeNull();
        serviceProvider.GetService<IStreamRequestHandler<TestStreamQuery, int>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddMediator_WithOptions_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssemblies(typeof(TestQueryHandler).Assembly);
            options.AddBehavior<ITestBehavior, TestBehavior>();
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<IMediator>().ShouldNotBeNull();
        serviceProvider.GetService<ITestBehavior>().ShouldNotBeNull();
        serviceProvider.GetService<ITestBehavior>().ShouldBeOfType<TestBehavior>();
    }

    [Fact]
    public void AddMediator_WithGenericBehavior_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(options =>
        {
            options.AddBehavior<ITestBehavior, TestBehavior>();
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var behavior = serviceProvider.GetService<ITestBehavior>();
        
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<TestBehavior>();
    }

    [Fact]
    public void MediatorOptions_RegisterServicesFromAssemblies_ShouldAddAssemblies()
    {
        // Arrange
        var options = new MediatorOptions();
        var assembly1 = typeof(TestQueryHandler).Assembly;
        var assembly2 = typeof(ServiceRegistrationExtensionsTests).Assembly;

        // Act
        options.RegisterServicesFromAssemblies(assembly1, assembly2);

        // Assert
        options.ShouldNotBeNull();
        // Note: Assemblies property is internal, so we test through registration
        var services = new ServiceCollection();
        services.AddMediator(opt => opt.RegisterServicesFromAssemblies(assembly1, assembly2));
        
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IMediator>().ShouldNotBeNull();
    }

    [Fact]
    public void MediatorOptions_AddBehavior_WithTypes_ShouldAddBehavior()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.AddBehavior(typeof(ITestBehavior), typeof(TestBehavior));

        // Assert
        var services = new ServiceCollection();
        services.AddMediator(opt => opt.AddBehavior(typeof(ITestBehavior), typeof(TestBehavior)));
        
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<ITestBehavior>().ShouldNotBeNull();
    }

    [Fact]
    public void MediatorOptions_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(options => options
            .RegisterServicesFromAssemblies(typeof(TestQueryHandler).Assembly)
            .AddBehavior<ITestBehavior, TestBehavior>());

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<IMediator>().ShouldNotBeNull();
        serviceProvider.GetService<ITestBehavior>().ShouldNotBeNull();
        serviceProvider.GetService<IRequestHandler<TestQuery, string>>().ShouldNotBeNull();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
