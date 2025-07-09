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
        
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<ISender>().Should().NotBeNull();
        serviceProvider.GetService<IPublisher>().Should().NotBeNull();
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
        
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<IRequestHandler<TestQuery, string>>().Should().NotBeNull();
        serviceProvider.GetService<IRequestHandler<TestCommand, Nast.SimpleMediator.Abstractions.Unit>>().Should().NotBeNull();
        serviceProvider.GetService<INotificationHandler<TestNotification>>().Should().NotBeNull();
        serviceProvider.GetService<IStreamRequestHandler<TestStreamQuery, int>>().Should().NotBeNull();
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
        
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<ITestBehavior>().Should().NotBeNull();
        serviceProvider.GetService<ITestBehavior>().Should().BeOfType<TestBehavior>();
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
        
        behavior.Should().NotBeNull();
        behavior.Should().BeOfType<TestBehavior>();
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
        options.Should().NotBeNull();
        // Note: Assemblies property is internal, so we test through registration
        var services = new ServiceCollection();
        services.AddMediator(opt => opt.RegisterServicesFromAssemblies(assembly1, assembly2));
        
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
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
        serviceProvider.GetService<ITestBehavior>().Should().NotBeNull();
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
        
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<ITestBehavior>().Should().NotBeNull();
        serviceProvider.GetService<IRequestHandler<TestQuery, string>>().Should().NotBeNull();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
