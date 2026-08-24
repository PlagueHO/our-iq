using Azure.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OurIQ.ToolServices;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class AzureCredentialFactoryTests
{
    [TestMethod]
    public void DevelopmentUsesDeveloperCredentialChain()
    {
        var credential = AzureCredentialFactory.Create(
            new TestHostEnvironment(Environments.Development),
            new AzureIdentityOptions());

        Assert.IsInstanceOfType<DefaultAzureCredential>(credential);
    }

    [TestMethod]
    public void HostedExecutionUsesManagedIdentity()
    {
        var systemAssigned = AzureCredentialFactory.Create(
            new TestHostEnvironment(Environments.Production),
            new AzureIdentityOptions());
        var userAssigned = AzureCredentialFactory.Create(
            new TestHostEnvironment(Environments.Production),
            new AzureIdentityOptions
            {
                ManagedIdentityClientId = "33333333-3333-3333-3333-333333333333"
            });

        Assert.IsInstanceOfType<ManagedIdentityCredential>(systemAssigned);
        Assert.IsInstanceOfType<ManagedIdentityCredential>(userAssigned);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = nameof(AzureCredentialFactoryTests);

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
