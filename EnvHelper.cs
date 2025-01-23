using DotNetEnv;

// Ensure you have the correct using directive for your .env library.

namespace PlaywrightApiTestingYouTube;

public static class EnvHelper
{
    private static bool _isEnvLoaded;
    
    // Gets the value of a key from the .env file.
    /// <param name="key">The key to fetch from the .env file.</param>
    /// <returns>The value of the key, or null if not found.</returns>
    public static string GetEnvVariable(string key)
    {
        if (!_isEnvLoaded)
        {
            LoadEnvFile();
            _isEnvLoaded = true;
        }
        return Env.GetString(key);
    }

    
    // Loads the .env file located at the project root.
    private static void LoadEnvFile()
    {
        var projectRoot = Path.Combine(Directory.GetCurrentDirectory(), "../../../");
        projectRoot = Path.GetFullPath(projectRoot); // Resolve to absolute path
        var envPath = Path.Combine(projectRoot, ".env");
        Env.Load(envPath);
    }
}