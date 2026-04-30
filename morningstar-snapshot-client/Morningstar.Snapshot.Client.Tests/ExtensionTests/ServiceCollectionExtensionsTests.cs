using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Morningstar.Snapshot.Client.Clients;
using Morningstar.Snapshot.Client.Extensions;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Client.Services.TokenProvider;

namespace Morningstar.Snapshot.Client.Tests.ExtensionTests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection services;

    public ServiceCollectionExtensionsTests()
    {
        services = new ServiceCollection();
        services.AddSnapshotServices();
    }

    // -------------------------------------------------------------------------
    // Registration — service is present
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(ISnapshotService))]
    [InlineData(typeof(ITokenProvider))]
    [InlineData(typeof(IApiHelper))]
    [InlineData(typeof(ISnapshotApiClient))]
    [InlineData(typeof(ISnapshotRequestFactory))]
    public void AddSnapshotServices_RegistersExpectedInterfaces(Type serviceType)
    {
        services.Should().Contain(d => d.ServiceType == serviceType);
    }

    // -------------------------------------------------------------------------
    // Registration — correct lifetime
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(ISnapshotService),        ServiceLifetime.Singleton)]
    [InlineData(typeof(ITokenProvider),          ServiceLifetime.Singleton)]
    [InlineData(typeof(ISnapshotApiClient),      ServiceLifetime.Singleton)]
    [InlineData(typeof(ISnapshotRequestFactory), ServiceLifetime.Singleton)]
    public void AddSnapshotServices_RegistersWithCorrectLifetime(Type serviceType, ServiceLifetime expectedLifetime)
    {
        services.Should().Contain(d =>
            d.ServiceType == serviceType &&
            d.Lifetime == expectedLifetime);
    }

    [Fact]
    public void AddSnapshotServices_RegistersApiHelper_WithTransientLifetime()
    {
        // AddHttpClient registers typed clients as Transient by default.
        services.Should().Contain(d =>
            d.ServiceType == typeof(IApiHelper) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    // -------------------------------------------------------------------------
    // Chaining — returns the same IServiceCollection
    // -------------------------------------------------------------------------

    [Fact]
    public void AddSnapshotServices_ReturnsServiceCollection_ForChaining()
    {
        var freshServices = new ServiceCollection();

        var result = freshServices.AddSnapshotServices();

        result.Should().BeSameAs(freshServices);
    }
}
