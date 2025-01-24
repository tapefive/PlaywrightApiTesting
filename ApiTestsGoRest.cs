using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit.Abstractions;
using static PlaywrightApiTesting.EnvHelper;
using Bogus;
using Serilog;

namespace PlaywrightApiTesting;

// Defines a test class for API testing using Playwright. Initialize Test Report class
public class ApiTestsGoRest(ITestOutputHelper testOutputHelper) : ExtentTestReport(testOutputHelper), IAsyncLifetime
{
    // Fields to hold Playwright instance, request context, created user ID, and a Faker instance
    private IPlaywright? _playwright;
    private IAPIRequestContext? _requestContext;
    private int _createdUserId;
    private static readonly Faker Faker = new();
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

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
                .MinimumLevel.Error()
                .WriteTo.TestOutput(_testOutputHelper)
                .WriteTo.File(Path.Combine(logDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            // Log start of test in test report
            Test.Info("Starting POST request to create a new user");

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
            // Log test successful in test report
            Test.Pass("Test passed");

            // Parse the response to retrieve the created user ID
            var jsonBody = await response.JsonAsync();
            var userResponse = jsonBody?.Deserialize<UserResponse>();
            Test.Info("Deserializing response body");
            Test.Info($"Body: {jsonBody}");
            _createdUserId = userResponse?.id ?? throw new Exception("Failed to retrieve user ID");
        }
        catch (Exception ex)
        {
            // Throw an exception if initialize fails and log it
            Log.Error(ex.ToString(), "Initialize failed");
            // Log test failed in test report
            Test.Fail("Test failed");
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
        FlushReports();
        await Log.CloseAndFlushAsync();
    }

    // Test to update the created user with new data
    [Fact]
    public async Task PutUpdateUserTest()
    {
        try
        {
            // Log start of test in test report
            Test.Info("Starting PUT request to update user");
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
            // Log test successful in test report
            Test.Pass("Test passed");

            // Log the updated user response
            var jsonBody = await response.JsonAsync();
            Test.Info("Deserializing response body");
            Test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            Test.Fail("Test failed");
            throw;
        }
    }

    // Test to fetch the created user's details
    [Fact]
    public async Task GetUserTest()
    {
        try
        {
            // Log start of test in test report
            Test.Info("Starting GET request to fetch user");
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
            // Log test successful in test report
            Test.Pass("Test passed");

            // Log the fetched user response
            var jsonBody = await response.JsonAsync();
            Test.Info("Deserializing response body");
            Test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            Test.Fail("Test failed");
            throw;
        }
    }

    // Test to delete the created user's details
    [Fact]
    public async Task DeleteUserTest()
    {
        try
        {
            // Log start of test in test report
            Test.Info("Starting DELETE request to delete user");
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
            // Log test successful in test report
            Test.Pass("Test passed");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            Test.Fail("Test failed");
            throw;
        }
    }

    // Test to fetch the deleted user's details
    [Fact]
    public async Task GetDeletedUserTest()
    {
        try
        {
            // Log start of test in test report
            Test.Info("Starting GET request to fetch deleted user");
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
            // Log test successful in test report
            Test.Pass("Test passed");
        }
        catch (Exception ex)
        {
            //Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            Test.Fail("Test failed");
            throw;
        }
    }
}