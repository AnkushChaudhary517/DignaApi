using Amazon.Lambda.AspNetCoreServer;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    // Called by the Lambda runtime to initialize the ASP.NET Core host
    protected override void Init(IHostBuilder builder)
    {
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
    }
}