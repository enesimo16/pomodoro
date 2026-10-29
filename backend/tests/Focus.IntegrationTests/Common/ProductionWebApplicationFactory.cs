namespace Focus.IntegrationTests.Common;

public class ProductionWebApplicationFactory : CustomWebApplicationFactory
{
    protected override string EnvironmentName => "Production";
}
