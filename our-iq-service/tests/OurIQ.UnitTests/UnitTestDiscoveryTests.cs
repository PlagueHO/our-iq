namespace OurIQ.UnitTests;

[TestClass]
public sealed class UnitTestDiscoveryTests
{
    [TestMethod]
    public void UnitTestAssemblyIsDiscoverable()
    {
        Assert.AreEqual("OurIQ.UnitTests", typeof(UnitTestDiscoveryTests).Assembly.GetName().Name);
    }
}
