using System.Linq;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests;

public class ClassroomControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ClassroomControllerTests(WebApplicationFactory<Program> factory)
    {
        var webFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Classroom");
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
    public async Task Create_Get_Update_Delete_Classroom()
    {
        var create = new { Building = "Main", RoomNumber = "101", Capacity = 30 };
        var createRes = await _client.PostAsJsonAsync("/api/classroom", create);
        createRes.EnsureSuccessStatusCode();
        var room = await createRes.Content.ReadFromJsonAsync<ClassroomDto>();
        Assert.NotNull(room);
        Assert.Equal("Main", room!.Building);
        Assert.Equal("101", room.RoomNumber);
        Assert.Equal(30, room.Capacity);

        var getRes = await _client.GetAsync($"/api/classroom/{room.Id}");
        getRes.EnsureSuccessStatusCode();
        var got = await getRes.Content.ReadFromJsonAsync<ClassroomDto>();
        Assert.Equal(room.Id, got!.Id);

        var update = new { Building = "North", RoomNumber = "202", Capacity = 40 };
        var putRes = await _client.PutAsJsonAsync($"/api/classroom/{room.Id}", update);
        putRes.EnsureSuccessStatusCode();
        var updated = await putRes.Content.ReadFromJsonAsync<ClassroomDto>();
        Assert.Equal("North", updated!.Building);
        Assert.Equal("202", updated.RoomNumber);
        Assert.Equal(40, updated.Capacity);

        var delRes = await _client.DeleteAsync($"/api/classroom/{room.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, delRes.StatusCode);

        var getAfter = await _client.GetAsync($"/api/classroom/{room.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getAfter.StatusCode);
    }
}


