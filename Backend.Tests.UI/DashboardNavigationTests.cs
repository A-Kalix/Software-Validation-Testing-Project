using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Xunit;

namespace Backend.Tests.UI;

public class DashboardNavigationTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "http://localhost:5173";

    public DashboardNavigationTests()
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
    public void LoginAndNavigateToCourses_ShouldOpenCourseCatalog()
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

        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        wait.Until(d => d.Url.Contains("/dashboard"));

        var courseLink = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.LinkText("Course Catalog")));
        courseLink.Click();

        wait.Until(d => d.Url.Contains("/dashboard/courses"));
        Assert.Contains("/dashboard/courses", _driver.Url);

        var header = _driver.FindElement(By.TagName("h1"));
        Assert.True(header.Text.Contains("Courses") || header.Text.Contains("Course"), "Expected course catalog page header.");
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
