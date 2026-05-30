using NetArchTest.Rules;
using Xunit;

namespace UnitTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "Core";
    private const string ApplicationNamespace = "Application";
    private const string InfrastructureNamespace = "Infrastructure";
    private const string ApiNamespace = "API";

    [Fact]
    public void Core_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(Core.TripPlanner.Trip).Assembly; // Wskazujemy dowolną klasę z projektu Core

        // Act
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Projekt Core nie może zależeć od innych warstw!");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOnInfrastructureOrApi()
    {
        // Arrange
        // (Zakładam, że masz tam interfejs ITripRepository, jeśli nie, podmień na dowolną klasę z Application)
        var assembly = typeof(Application.Messages.IMessagingService).Assembly;

        // Act
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Projekt Application nie może zależeć od bazy danych ani API!");
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependencyOnApi()
    {
        // Arrange
        var assembly = typeof(Infrastructure.AppDbContext).Assembly;

        // Act
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Projekt Infrastructure nie może zależeć od API!");
    }
}