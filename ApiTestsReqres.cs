using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;
using System.Text.Json;
using FluentAssertions;
using Serilog;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Microsoft.Playwright;

namespace PlaywrightApiTesting;

// Defines a test class for API testing using Playwright
// Constructor that accepts a test output helper for logging
public class ApiTestsReqres(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    // Fields to hold Playwright and API request context instances
    private IPlaywright? _playwright;
    private IAPIRequestContext? _requestContext;
    private IPage? _page;
    private IBrowser? _browser;
    
    // Static instance of ExtentReports to be shared across tests
    private static readonly AventStack.ExtentReports.ExtentReports ExtentInstance;
    
    // Test node that represents each test method in the Extent report
    private readonly ExtentTest _test = ExtentInstance.CreateTest(testOutputHelper.GetType().Name);

    static ApiTestsReqres()
    {
        var baseDirectory = AppContext.BaseDirectory;
        // Define the directory where the reports will be saved
        var reportDirectory = Path.Combine(baseDirectory, "../../../REQRESTestReports/");
            
        // Convert relative path to an absolute path
        reportDirectory = Path.GetFullPath(reportDirectory);

        // Ensure the directory exists
        if (!Directory.Exists(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        // Create a new HTML reporter that writes the report to the specified directory
        var htmlReporter = new ExtentHtmlReporter(reportDirectory)
        {
            Config =
            {
                DocumentTitle = "REQRES Playwright API Test Report",  // Title displayed in the HTML document
                ReportName = "REQRES Playwright API Tests",          // Name of the report
                Theme = AventStack.ExtentReports.Reporter.Configuration.Theme.Dark // Dark theme for the report
            }
        };

        // Initialize the ExtentReports instance and attach the HTML reporter to it
        ExtentInstance = new AventStack.ExtentReports.ExtentReports();
        ExtentInstance.AttachReporter(htmlReporter);
    }

    // Method to flush the reports once all tests are done, ensuring data is written to the file
    private static void FlushReports()
    {
        // Flush reports to disk
        ExtentInstance.Flush();
    }
    
    // Asynchronous initialization method to set up Playwright and request context
    public async Task InitializeAsync()
    {
        try
        {
            // Create a Playwright instance
            _playwright = await Playwright.CreateAsync();
            
            // Initialize the browser and page for UI testing
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            _page = await _browser.NewPageAsync();

            // Create a new API request context
            _requestContext = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions()
            {
                BaseURL = "https://reqres.in"
            });

            // Configure logger globally and locate log directory for writing the logs to
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../../../", "TestLogs");
            logDirectory = Path.GetFullPath(logDirectory);

            // Ensure the log directory exists
            Directory.CreateDirectory(logDirectory);

            // Create a Serilog instance and log the output of tests to a .txt file in the TestLogs directory if an error occurs
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.TestOutput(testOutputHelper)
                .WriteTo.File(Path.Combine(logDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        catch (Exception ex)
        {
            // Throw an exception initialize fails and log it
            Log.Error(ex.ToString(), "Initialize failed");
            throw;
        }
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
        await Log.CloseAndFlushAsync();
        FlushReports();
    }

    // Test for performing a GET request for a single user
    [Fact]
    public async Task GetSingleUserTest()
    {
        try
        {
            // Log start of test in test report
            _test.Info("Starting GET request to fetch user");
            // Perform a GET request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.GetAsync("/api/users/2");

            // Verify that the HTTP status code is 200 (OK)
            Assert.Equal(200, response.Status);
            // Log test successful in test report
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the expected data
            Assert.Contains("Janet", bodyString);

            // Parse the response as JSON and log it in the report
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to create a user
    [Fact]
    public async Task PostCreateUserTest()
    {
        try
        {
            // Log start of test in test report
            _test.Info("Starting POST request to create user");
            // Data to send in the POST request
            var postData = new
            {
                name = "Test User",
                job = "2024: QA Analyst"
            };

            // Perform a POST request with the specified data
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/users", new APIRequestContextOptions()
            {
                DataObject = postData
            });

            // Verify that the HTTP status code is 201 (Created)
            Assert.Equal(201, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the data
            Assert.Contains("Test User", bodyString);
            Assert.Contains("2024: QA Analyst", bodyString);

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
                _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a PUT request to update a user
    [Fact]
    public async Task PutUpdateUserTest()
    {
        try
        {
            _test.Info("Starting PUT request to update user");
            // Data to send in the PUT request
            var postData = new
            {
                name = "Update Test User",
                job = "2025: Software Developer in Test"
            };

            // Perform a PUT request with the specified data
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PutAsync("/api/users/2", new APIRequestContextOptions()
            {
                DataObject = postData
            });

            // Verify that the HTTP status code is 200 (OK)
            Assert.Equal(200, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the updated data
            Assert.Contains("Update Test User", bodyString);
            Assert.Contains("2025: Software Developer in Test", bodyString);

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }
    
    // Test for performing a DELETE request to delete a user
    [Fact]
    public async Task DeleteUserTest()
    {
        try
        {
            _test.Info("Starting DELETE request to delete user");
            // Perform a DELETE request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.DeleteAsync("/api/users/2");

            // Verify that the HTTP status code is 204 (No Content)
            Assert.Equal(204, response.Status);
            _test.Pass("Test passed");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to register and obtain a token
    [Fact]
    public async Task GoodRegisterTest()
    {
        _test.Info("Starting POST request to register user");
        try
        {
            // Perform a successful POST request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/register", new APIRequestContextOptions
            {
                DataObject = new
                {
                    email = "eve.holt@reqres.in",
                    password = "pistol"
                }
            });

            // Verify that the HTTP status code is 200 (OK)
            Assert.Equal(200, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the updated data
            Assert.Contains("token", bodyString);
            Assert.Contains("QpwL5tke4Pnpja7X4", bodyString);

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to attempt to register without a password
    [Fact]
    public async Task BadRegisterTest()
    {
        _test.Info("Starting POST request to register user without a password");
        try
        {
            // Perform an unsuccessful POST request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/register", new APIRequestContextOptions
            {
                DataObject = new
                {
                    email = "eve.holt@reqres.in"
                }
            });

            // Verify that the HTTP status code is 400 (Bad Request)
            Assert.Equal(400, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the updated data
            Assert.Contains("error", bodyString);
            Assert.Contains("Missing password", bodyString);

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to log in and obtain a token
    [Fact]
    public async Task GoodLoginTest()
    {
        _test.Info("Starting POST request to log in");
        try
        {
            // Perform a successful POST request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/login", new APIRequestContextOptions
            {
                DataObject = new
                {
                    email = "eve.holt@reqres.in",
                    password = "cityslicka"
                }
            });

            // Verify that the HTTP status code is 200 (OK)
            Assert.Equal(200, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the updated data
            Assert.Contains("token", bodyString);
            Assert.Contains("QpwL5tke4Pnpja7X4", bodyString);

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        // Throw an exception if test fails and log it
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to attempt to log in without a password
    [Fact]
    public async Task BadLoginTest()
    {
        _test.Info("Starting POST request to attempt to log in with no password");
        try
        {
            // Perform an unsuccessful POST request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/login", new APIRequestContextOptions
            {
                DataObject = new
                {
                    email = "sean@test.com"
                }
            });

            // Verify that the HTTP status code is 400 (Bad Request)
            Assert.Equal(400, response.Status);
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the updated data
            Assert.Contains("error", bodyString);
            Assert.Contains("Missing password", bodyString);
            _test.Pass("Test passed");

            // Parse the response as JSON and log it
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a GET request to list all users
    [Fact]
    public async Task GetListAllUsersTest()
    {
        _test.Info("Starting GET request to list all users");
        try
        {
            // Perform a GET request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.GetAsync("/api/users?page=2");

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
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");

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
                Assert.True(dataItem.TryGetProperty("avatar", out _),
                    "The 'avatar' property is missing in a data item.");
            }

            // Validate the "support" object properties
            var supportElement = rootElement.GetProperty("support");
            Assert.True(supportElement.TryGetProperty("url", out _),
                "The 'url' property is missing in the support object.");
            Assert.True(supportElement.TryGetProperty("text", out _),
                "The 'text' property is missing in the support object.");
            _test.Pass("Test passed");

        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    // Test for performing a POST request to register and obtain a token
    [Fact]
    public async Task AuthenticateTest()
    {
        _test.Info("Starting POST request to register user");
        try
        {
            // Perform a successful POST request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.PostAsync("/api/register", new APIRequestContextOptions
            {
                DataObject = new
                {
                    email = "eve.holt@reqres.in",
                    password = "pistol"
                }
            });

            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");

            var authResponse = jsonBody?.Deserialize<Authenticate>();
            
            authResponse?.token.Should().NotBe(string.Empty);
            authResponse?.token.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            _test.Fail("Test failed");
            throw;
        }
    }

    
    private class Authenticate
    {
        public string token { get; set; } = null!;
    }

    [Fact]

    public async Task CombinedFrontEndApiTest()
    {
        try
        { 
            _test.Info("Starting test to navigate to home page");
            // Navigate to home page URL
            await _page!.GotoAsync("https://reqres.in");
            // Verify home page logo is present
            _page.Locator("https://reqres.in/img/logo.png");
            // Assert that the header text matches the expected value
            // Log start of test in test report
            _test.Info("Starting GET request to fetch user");
            // Perform a GET request to the specified URL
            Debug.Assert(_requestContext != null, nameof(_requestContext) + " != null");
            var response = await _requestContext.GetAsync("/api/users/2");

            // Verify that the HTTP status code is 200 (OK)
            Assert.Equal(200, response.Status);
            // Log test successful in test report
            _test.Pass("Test passed");

            // Read and decode the response body
            var body = await response.BodyAsync();
            var bodyString = Encoding.UTF8.GetString(body);

            // Verify that the response contains the expected data
            Assert.Contains("Janet", bodyString);

            // Parse the response as JSON and log it in the report
            var jsonBody = await response.JsonAsync();
            _test.Info("Deserializing response body");
            _test.Info($"Body: {jsonBody}");
        }
        catch (Exception ex)
        {
            // Throw an exception if test fails and log it
            Log.Error(ex.ToString(), "Test failed");
            // Log test failed in test report
            _test.Fail("Test failed");
            throw;
        }
    }
}
