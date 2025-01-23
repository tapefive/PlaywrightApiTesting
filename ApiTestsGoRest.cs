using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit.Abstractions;
using static PlaywrightApiTesting.EnvHelper;
using Bogus;
using Serilog;

namespace PlaywrightApiTesting;

// Defines a test class for API testing using Playwright
public class ApiTestsGoRest(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    // Fields to hold Playwright instance, request context, created user ID, and a Faker instance
    private IPlaywright? _playwright;
    private IAPIRequestContext? _requestContext;
    private int _createdUserId;
    private static readonly Faker Faker = new();

    // Class to represent a user response from the API
    private class UserResponse
    {
        public int id { get; set; }
    }

    // Generates random user data using the Faker library
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

    // Initializes Playwright and sets up API request context
    public async Task InitializeAsync()
    {
        try
        {
            // Create Playwright instance
            _playwright = await Playwright.CreateAsync();

            // Set up API request context with the base URL
            _requestContext = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions()
            {
                BaseURL = "https://gorest.co.in"
            });

            // Configure logger globally
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../../../", "TestLogs");
            logDirectory = Path.GetFullPath(logDirectory);

            // Ensure the log directory exists
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(testOutputHelper)
                .WriteTo.File(Path.Combine(logDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Logger initialized. Test execution starting.");

            // Retrieve access token from environment variables
            var accessToken = GetEnvVariable("ACCESS_TOKEN");

            // Generate random user data for the request
            var postData = GenerateRandomUser();

            // Send a POST request to create a new user
            var response = await _requestContext.PostAsync("/public/v2/users", new APIRequestContextOptions()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {accessToken}" }
                },
                DataObject = postData
            });

            // Assert that the response status is 201 (Created)
            Assert.Equal(201, response.Status);

            // Parse the response to retrieve the created user ID
            var jsonBody = await response.JsonAsync();
            var userResponse = jsonBody?.Deserialize<UserResponse>();
            _createdUserId = userResponse?.id ?? throw new Exception("Failed to retrieve user ID");

            // Log the created user ID
            Log.Information($"Created User ID during initialization: {_createdUserId}");
        }
        catch (Exception ex)
        {
            //Throw an exception if initialize fails and log it
            Log.Error(ex.ToString(), "Initialize failed");
            throw;
        }
    }

    // Disposes of resources when the test class is done
    public async Task DisposeAsync()
    {
        if (_requestContext != null)
        {
            await _requestContext.DisposeAsync();
        }

        _playwright?.Dispose();
        await Log.CloseAndFlushAsync();
    }

    // Test to update the created user with new data
    [Fact]
    public async Task PutUpdateUserTest()
    {
        try
        {
            // Retrieve access token from environment variables
            var accessToken = GetEnvVariable("ACCESS_TOKEN");

            // Generate new user data for the PUT request
            var putData = new
            {
                name = Faker.Name.FullName(),
                gender = Faker.PickRandom("Male", "Female"),
                email = Faker.Internet.Email(),
                status = "active"
            };

            // Send a PUT request to update the user
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PutAsync($"/public/v2/users/{_createdUserId}",
                new APIRequestContextOptions()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "Authorization", $"Bearer {accessToken}" }
                    },
                    DataObject = putData
                });

            // Assert that the response status is 200 (OK)
            Assert.Equal(200, response.Status);

            // Log the updated user response
            var jsonBody = await response.JsonAsync();
            Log.Information($"Updated User Response: {jsonBody}");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            throw;
        }
    }

    // Test to fetch the created user's details
    [Fact]
    public async Task GetUserTest()
    {
        try
        {
            // Retrieve access token from environment variables
            var accessToken = GetEnvVariable("ACCESS_TOKEN");

            // Send a GET request to retrieve the user's details
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.GetAsync($"/public/v2/users/{_createdUserId}",
                new APIRequestContextOptions()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "Authorization", $"Bearer {accessToken}" }
                    }
                });

            // Assert that the response status is 200 (OK)
            Assert.Equal(200, response.Status);

            // Log the fetched user response
            var jsonBody = await response.JsonAsync();
            Log.Information($"Fetched User Response: {jsonBody}");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            throw;
        }
    }

    // Test to delete the created user's details
    [Fact]
    public async Task DeleteUserTest()
    {
        try
        {
            // Retrieve access token from environment variables
            var accessToken = GetEnvVariable("ACCESS_TOKEN");

            // Send a DELETE request to delete the user's details
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.DeleteAsync($"/public/v2/users/{_createdUserId}",
                new APIRequestContextOptions()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "Authorization", $"Bearer {accessToken}" }
                    }
                });

            // Assert that the response status is 204 (Successful)
            Assert.Equal(204, response.Status);

        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            throw;
        }
    }

    // Test to fetch the deleted user's details
    [Fact]
    public async Task GetDeletedUserTest()
    {
        try
        {

            // Retrieve access token from environment variables
            var accessToken = GetEnvVariable("ACCESS_TOKEN");

            // Send a GET request to retrieve the user's details
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.GetAsync($"/public/v2/users/7614801", new APIRequestContextOptions()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "Authorization", $"Bearer {accessToken}" }
                }
            });

            // Assert that the response status is 404 (Not found)
            Assert.Equal(404, response.Status);
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            throw;
        }
    }
}
