using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace Users.Tests;

public class ArchitectureTests
{
    private const string CoreNamespace = "Users.Core";
    private const string ApplicationNamespace = "Users.Application";
    private const string InfrastructureNamespace = "Users.Infrastructure";
    private const string ApiNamespace = "Users.API";

    [Fact]
    public void Core_Should_Not_Have_Dependency_On_Other_Layers()
    {
        var result = Types.InNamespace(CoreNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Core layer should not depend on any other layers.");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure_Or_API()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application layer should not depend on Infrastructure or API layers.");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependency_On_API()
    {
        var result = Types.InNamespace(InfrastructureNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Infrastructure layer should not depend on API layer.");
    }
}
