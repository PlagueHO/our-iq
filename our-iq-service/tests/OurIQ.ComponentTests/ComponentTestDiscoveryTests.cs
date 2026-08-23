namespace OurIQ.ComponentTests;

[TestClass]
public sealed class ComponentTestDiscoveryTests
{
    [TestMethod]
    public void ComponentTestAssemblyIsDiscoverable()
    {
        Assert.AreEqual("OurIQ.ComponentTests", typeof(ComponentTestDiscoveryTests).Assembly.GetName().Name);
    }
}
