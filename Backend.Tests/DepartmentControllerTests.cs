using System.Linq;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests;

public class DepartmentControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

        public DepartmentControllerTests(WebApplicationFactory<Program> factory)
    {
        var webFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Department");
                });

                    // Replace authentication with test scheme that auto-authenticates as Admin
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });

        _client = webFactory.CreateClient();
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Department()
    {
        var create = new { Name = "Test Dept", Code = "TD" };
        var createRes = await _client.PostAsJsonAsync("/api/department", create);
        createRes.EnsureSuccessStatusCode();
        var dept = await createRes.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.NotNull(dept);
        Assert.Equal("Test Dept", dept!.Name);
        Assert.Equal("TD", dept.Code);

        var getRes = await _client.GetAsync($"/api/department/{dept.Id}");
        getRes.EnsureSuccessStatusCode();
        var got = await getRes.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.Equal(dept.Id, got!.Id);

        var update = new { Name = "Updated Dept", Code = "UD" };
        var putRes = await _client.PutAsJsonAsync($"/api/department/{dept.Id}", update);
        putRes.EnsureSuccessStatusCode();
        var updated = await putRes.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.Equal("Updated Dept", updated!.Name);
        Assert.Equal("UD", updated.Code);

        var delRes = await _client.DeleteAsync($"/api/department/{dept.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, delRes.StatusCode);

        var getAfter = await _client.GetAsync($"/api/department/{dept.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getAfter.StatusCode);
    }
}


