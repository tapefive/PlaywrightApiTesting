using Microsoft.Playwright;
using Xunit.Abstractions;
using DotNetEnv;


namespace PlaywrightApiTestingYouTube;

// Defines a test class for API testing using Playwright
// Constructor that accepts a test output helper for logging
public class ApiTestsGoRest(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    // Fields to hold Playwright and API request context instances
    private IPlaywright? _playwright;
    private IAPIRequestContext? _requestContext;

    // Asynchronous initialization method to set up Playwright and request context
    public async Task InitializeAsync()
    {
        // Create a Playwright instance
        _playwright = await Playwright.CreateAsync();

        // Create a new API request context
        _requestContext = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions()
        {
            BaseURL = "https://gorest.co.in"
        });
    }

    // Asynchronous cleanup method to dispose of resources
    public async Task DisposeAsync()
    {
        // Dispose of the request context if it was initialized
        if (_requestContext != null)
        {
            await _requestContext.DisposeAsync();
        }

        // Dispose of the Playwright instance
        _playwright?.Dispose();
    }
    
    // Test for performing a POST request to create a user
    [Fact]
    public async Task PostCreateUserTest()
    {
        // Get ACCESS_TOKEN from .env file
        var projectRoot = Path.Combine(Directory.GetCurrentDirectory(), "../../../");
        projectRoot = Path.GetFullPath(projectRoot); // Resolve to absolute path
        var envPath = Path.Combine(projectRoot, ".env");
        Env.Load(envPath);
        var accessToken = Env.GetString("ACCESS_TOKEN");
        
        // Data to send in the POST request
        var postData = new
        {
            name = "Test User 5",
            gender = "Male",
            email = "test5@tester.co.uk",
            status = "active"
        };
        // Perform a POST request with the specified data
        var response = await _requestContext!.PostAsync("/public/v2/users", new APIRequestContextOptions()
        {
            Headers = new Dictionary<string, string>()
            {
                {"Authorization", $"Bearer {accessToken}"}
            },
            DataObject = postData
        });
        
        // Verify that the HTTP status code is 201 (Created)
        Assert.Equal(201, response.Status);
        
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }
}
