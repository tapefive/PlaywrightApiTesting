using System.Text.Json;
using Microsoft.Playwright;
using Xunit.Abstractions;
using FluentAssertions;
using static PlaywrightApiTestingYouTube.EnvHelper;
using Bogus;


namespace PlaywrightApiTestingYouTube;

// Defines a test class for API testing using Playwright
// Constructor that accepts a test output helper for logging
public class ApiTestsGoRest(ITestOutputHelper testOutputHelper) : IAsyncLifetime

{
    // Fields to hold Playwright and API request context instances
    private IPlaywright? _playwright;
    private IAPIRequestContext? _requestContext;
    private int _createdUserId;
    private static readonly Faker Faker = new();
    
    private class UserResponse
    {
        public int id { get; set; }
    }

    private object GenerateRandomUser()
    {
        return new
        {
            name = Faker.Name.FullName(),
            gender = Faker.PickRandom("Male", "Female"),
            email = Faker.Internet.Email(),
            status = Faker.PickRandom("active", "inactive")

        };
    }

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
        // Dispose of the request context if it was Initialized
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
        
        // Calls helper method to retrieve access token from environment variables
        var accessToken = GetEnvVariable("ACCESS_TOKEN");
        accessToken.Should().NotBeNull();
        
        // Data to send in the POST request
        var postData = GenerateRandomUser();
        
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
        testOutputHelper.WriteLine(jsonBody?.ToString());

        var userResponse = jsonBody?.Deserialize<UserResponse>();
        if (userResponse != null) _createdUserId = userResponse.id;
        testOutputHelper.WriteLine($"Created User ID: {_createdUserId}");


    }
    
    // Test for performing a PUT request to update a created user
    [Fact]
    public async Task PutUpdateUserTest()
    { 
        // Calls helper method to retrieve access token from environment variables
        var accessToken = GetEnvVariable("ACCESS_TOKEN");
        accessToken.Should().NotBeNull();
        
        // Data to send in the PUT request
        var putData = new
        {
            name = Faker.Name.FullName(),
            gender = Faker.PickRandom("Male", "Female"),
            email = Faker.Internet.Email(),
            status = Faker.PickRandom("active")
        };
        
        // Perform a PUT request with the specified data and the user id
        var response = await _requestContext!.PutAsync($"/public/v2/users/7610625", new APIRequestContextOptions()
        {
            Headers = new Dictionary<string, string>()
            {
                {"Authorization", $"Bearer {accessToken}"}
            },
            DataObject = putData
        });
        
        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);
        
        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }
    
    // Test for performing a GET request to retrieve user details
    [Fact]
    public async Task GetUserTest()

    {
        // Calls helper method to retrieve access token from environment variables
        var accessToken = GetEnvVariable("ACCESS_TOKEN");
        accessToken.Should().NotBeNull();
        
        // Perform a GET request with the user id
        var response = await _requestContext!.GetAsync($"/public/v2/users/7610625",
            new APIRequestContextOptions()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {accessToken}" }
                },
            });

        // Verify that the HTTP status code is 200 (OK)
        Assert.Equal(200, response.Status);

        var jsonBody = await response.JsonAsync();
        testOutputHelper.WriteLine(jsonBody.ToString());
    }
}
