using System.Net;
using System.Net.Http.Json;
using CarInsurance.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarInsurance.Api.Tests;

public class InsuranceValidityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InsuranceValidityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("01/06/2024")]
    [InlineData("2024-2-3")]
    [InlineData("2024-02-30")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidDate_Returns400(string date)
    {
        var resp = await _client.GetAsync($"/api/cars/1/insurance-valid?date={date}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task MissingCar_Returns404()
    {
        var resp = await _client.GetAsync("/api/cars/999999/insurance-valid?date=2024-06-01");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Theory]
    [InlineData("2024-01-01", true)]
    [InlineData("2024-12-31", true)]
    [InlineData("2023-12-31", false)]
    [InlineData("2025-06-10", true)]
    public async Task Car1_Boundaries(string date, bool expectedValid)
    {
        var dto = await _client.GetFromJsonAsync<InsuranceValidityResponse>(
            $"/api/cars/1/insurance-valid?date={date}");
        Assert.NotNull(dto);
        Assert.Equal(expectedValid, dto!.Valid);
    }

    [Theory]
    [InlineData("2025-09-30", true)]
    [InlineData("2025-10-01", false)]
    public async Task Car2_Boundaries(string date, bool expectedValid)
    {
        var dto = await _client.GetFromJsonAsync<InsuranceValidityResponse>(
            $"/api/cars/2/insurance-valid?date={date}");
        Assert.NotNull(dto);
        Assert.Equal(expectedValid, dto!.Valid);
    }
}
