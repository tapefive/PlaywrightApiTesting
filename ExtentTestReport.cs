using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Xunit.Abstractions;

namespace PlaywrightApiTesting
{
    public abstract class ExtentTestReport(ITestOutputHelper testOutputHelper)
    {
        // Static instance of ExtentReports to be shared across tests
        private static readonly AventStack.ExtentReports.ExtentReports ExtentInstance;

        // Test node that represents each test method in the Extent report
        protected readonly ExtentTest Test = ExtentInstance.CreateTest(testOutputHelper.GetType().Name);

        // Static constructor to initialize the reporting system once
        static ExtentTestReport()
        {
            
            var baseDirectory = AppContext.BaseDirectory;
            // Define the directory where the reports will be saved
            var reportDirectory = Path.Combine(baseDirectory, "../../../GORestTestReports/");
            
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
                    DocumentTitle = "GORest Playwright API Test Report",  // Title displayed in the HTML document
                    ReportName = "GORest Playwright API Tests",          // Name of the report
                    Theme = AventStack.ExtentReports.Reporter.Configuration.Theme.Dark // Dark theme for the report
                }
            };

            // Initialize the ExtentReports instance and attach the HTML reporter to it
            ExtentInstance = new AventStack.ExtentReports.ExtentReports();
            ExtentInstance.AttachReporter(htmlReporter);
        }

        // Method to flush the reports once all tests are done, ensuring data is written to the file
        protected static void FlushReports()
        {
            // Flush reports to disk
            ExtentInstance.Flush();
        }
    }
}
