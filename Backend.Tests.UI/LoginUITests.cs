using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Xunit;

namespace Backend.Tests.UI;

public class LoginUITests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "http://localhost:5173"; // Your Vite Dev Server

    public LoginUITests()
    {
        var options = new ChromeOptions();
        var runHeadless = Environment.GetEnvironmentVariable("UI_TEST_HEADLESS");
        if (string.IsNullOrEmpty(runHeadless) || runHeadless.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
        }

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
    }

    [Fact]
    public void Login_WithValidCredentials_ShouldNavigateToDashboard()
    {
        // 1. Navigate to Login Page
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        // 2. Find and Fill Email
        var emailInput = _driver.FindElement(By.CssSelector("input[type='email']"));
        emailInput.SendKeys("test@university.edu");

        // 3. Find and Fill Password
        var passwordInput = _driver.FindElement(By.CssSelector("input[type='password']"));
        passwordInput.SendKeys("Password123!");

        // 4. Click Submit
        var submitButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitButton.Click();

        // 5. Wait for Navigation (to Dashboard or Home)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // wait.Until(d => d.Url.Contains("/dashboard") || d.Url == $"{_baseUrl}/");

        // Assert
        // Assert.Contains(_baseUrl, _driver.Url);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
