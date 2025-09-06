using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return BadRequest("Invalid date. Use strict format YYYY-MM-DD");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> AddClaim(long carId, [FromBody] CreateClaimRequest request)
    {
        try
        {
            var created = await _service.AddClaimAsync(carId, request);
            return Created($"/api/cars/{carId}/claims/{created.Id}", created);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<CarHistoryEntry>>> GetHistory(long carId)
    {
        try
        {
            var items = await _service.GetHistoryAsync(carId);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

}
