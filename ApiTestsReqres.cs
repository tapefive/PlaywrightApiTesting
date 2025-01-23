using System.Text;
using Microsoft.Playwright;
using Xunit.Abstractions;
using System.Text.Json;
using FluentAssertions;

namespace PlaywrightApiTestingYouTube;

// Defines a test class for API testing using Playwright
// Constructor that accepts a test output helper for logging
public class ApiTestsReqres(ITestOutputHelper testOutputHelper) : IAsyncLifetime
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
            BaseURL = "https://reqres.in"
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

    // Test for performing a GET request for a single user
    [Fact]
    public async Task GetSingleUserTest()
    {
        // Perform a GET request to the specified URL
        var response = await _requestContext!.GetAsync("/api/users/2");

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the expected data
        Assert.Contains("Janet", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a POST request to create a user
    [Fact]
    public async Task PostCreateUserTest()
    {
        // Data to send in the POST request
        var postData = new
        {
            name = "Test User",
            job = "2024: QA Analyst"
        };

        // Perform a POST request with the specified data
        var response = await _requestContext!.PostAsync("/api/users", new APIRequestContextOptions()
        {
            DataObject = postData
        });

        // Verify that the HTTP status code is 201 (Created)
        Assert.Equal(201, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the data
        Assert.Contains("Test User", bodyString);
        Assert.Contains("2024: QA Analyst", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a PUT request to update a user
    [Fact]
    public async Task PutUpdateUserTest()
    {
        // Data to send in the PUT request
        var postData = new
        {
            name = "Update Test User",
            job = "2025: Software Developer in Test"
        };

        // Perform a PUT request with the specified data
        var response = await _requestContext!.PutAsync("/api/users/2", new APIRequestContextOptions()
        {
            DataObject = postData
        });

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the updated data
        Assert.Contains("Update Test User", bodyString);
        Assert.Contains("2025: Software Developer in Test", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a DELETE request to delete a user
    [Fact]
    public async Task DeleteUserTest()
    {
        // Perform a DELETE request to the specified URL
        var response = await _requestContext!.DeleteAsync("/api/users/2");

        // Verify that the HTTP status code is 204 (No Content)
        Assert.Equal(204, response.Status);
    }

    // Test for performing a POST request to register and obtain a token
    [Fact]
    public async Task GoodRegisterTest()
    {
        // Perform a successful POST request to the specified URL
        var response = await _requestContext!.PostAsync("/api/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "eve.holt@reqres.in",
                password = "pistol"
            }
        });

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the updated data
        Assert.Contains("token", bodyString);
        Assert.Contains("QpwL5tke4Pnpja7X4", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a POST request to attempt to register without a password
    [Fact]
    public async Task BadRegisterTest()
    {
        // Perform an unsuccessful POST request to the specified URL
        var response = await _requestContext!.PostAsync("/api/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "eve.holt@reqres.in"
            }
        });

        // Verify that the HTTP status code is 400 (Bad Request)
        Assert.Equal(400, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the updated data
        Assert.Contains("error", bodyString);
        Assert.Contains("Missing password", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a POST request to log in and obtain a token
    [Fact]
    public async Task GoodLoginTest()
    {
        // Perform a successful POST request to the specified URL
        var response = await _requestContext!.PostAsync("/api/login", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "eve.holt@reqres.in",
                password = "cityslicka"
            }
        });

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the updated data
        Assert.Contains("token", bodyString);
        Assert.Contains("QpwL5tke4Pnpja7X4", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a POST request to attempt to log in without a password
    [Fact]
    public async Task BadLoginTest()
    {
        // Perform an unsuccessful POST request to the specified URL
        var response = await _requestContext!.PostAsync("/api/login", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "sean@test.com"
            }
        });

        // Verify that the HTTP status code is 400 (Bad Request)
        Assert.Equal(400, response.Status);

        // Read and decode the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Verify that the response contains the updated data
        Assert.Contains("error", bodyString);
        Assert.Contains("Missing password", bodyString);

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }

    // Test for performing a GET request to list all users
    [Fact]
    public async Task GetListAllUsersTest()
    {
        // Perform a GET request to the specified URL
        var response = await _requestContext!.GetAsync("/api/users?page=2");

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        // Parse and validate the response body
        var body = await response.BodyAsync();
        var bodyString = Encoding.UTF8.GetString(body);

        // Parse JSON
        var jsonDocument = JsonDocument.Parse(bodyString);
        var rootElement = jsonDocument.RootElement;

        // Parse the response as JSON and log it
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());

        // Assert top-level properties
        Assert.True(rootElement.TryGetProperty("page", out _), "The 'page' property is missing.");
        Assert.True(rootElement.TryGetProperty("per_page", out _), "The 'per_page' property is missing.");
        Assert.True(rootElement.TryGetProperty("total", out _), "The 'total' property is missing.");
        Assert.True(rootElement.TryGetProperty("total_pages", out _), "The 'total_pages' property is missing.");
        Assert.True(rootElement.TryGetProperty("data", out _), "The 'data' property is missing.");
        Assert.True(rootElement.TryGetProperty("support", out _), "The 'support' property is missing.");

        // Validate the "data" array properties
        foreach (var dataItem in rootElement.GetProperty("data").EnumerateArray())
        {
            Assert.True(dataItem.TryGetProperty("id", out _), "The 'id' property is missing in a data item.");
            Assert.True(dataItem.TryGetProperty("email", out _), "The 'email' property is missing in a data item.");
            Assert.True(dataItem.TryGetProperty("first_name", out _),
                "The 'first_name' property is missing in a data item.");
            Assert.True(dataItem.TryGetProperty("last_name", out _),
                "The 'last_name' property is missing in a data item.");
            Assert.True(dataItem.TryGetProperty("avatar", out _), "The 'avatar' property is missing in a data item.");
        }

        // Validate the "support" object properties
        var supportElement = rootElement.GetProperty("support");
        Assert.True(supportElement.TryGetProperty("url", out _),
            "The 'url' property is missing in the support object.");
        Assert.True(supportElement.TryGetProperty("text", out _),
            "The 'text' property is missing in the support object.");

    }

    // Test for performing a POST request to register and obtain a token
    [Fact]
    public async Task AuthenticateTest()
    {
        // Perform a successful POST request to the specified URL
        var response = await _requestContext!.PostAsync("/api/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "eve.holt@reqres.in",
                password = "pistol"
            }
        });

        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());

        var authResponse = jsonBody?.Deserialize<Authenticate>();

        // var token = jsonBody?.GetProperty("token").ToString();
        authResponse?.token.Should().NotBe(string.Empty);
        authResponse?.token.Should().NotBeNull();
    }

    private class Authenticate
    {
        public string token { get; set; }
    }
}
