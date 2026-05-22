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
        _driver.Navigate().GoToUrl($"{_baseUrl}/login");

        var loginEmail = Environment.GetEnvironmentVariable("UI_TEST_USERNAME") ?? "student@university.edu";
        var loginPassword = Environment.GetEnvironmentVariable("UI_TEST_PASSWORD") ?? "Demo123!";

        var emailInput = _driver.FindElement(By.CssSelector("input[type='email']"));
        emailInput.Clear();
        emailInput.SendKeys(loginEmail);

        var passwordInput = _driver.FindElement(By.CssSelector("input[type='password']"));
        passwordInput.Clear();
        passwordInput.SendKeys(loginPassword);

        var submitButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitButton.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d => d.Url.Contains("/dashboard"));

        Assert.Contains("/dashboard", _driver.Url);
        Assert.NotNull(_driver.FindElement(By.CssSelector("nav")));
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
