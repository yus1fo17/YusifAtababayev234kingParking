using Microsoft.AspNetCore.Mvc;
using ParkingApi.Models;
using ParkingApi.Services;

namespace ParkingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingController : ControllerBase
{
    private readonly OverpassService _overpass;

    public ParkingController(OverpassService overpass)
    {
        _overpass = overpass;
    }

    /// <summary>
    /// Verilən koordinata yaxın parkingləri qaytarır.
    /// </summary>
    /// <remarks>
    /// Nümunə sorğu:
    ///
    ///     POST /api/parking/nearby
    ///     {
    ///         "latitude": 40.4093,
    ///         "longitude": 49.8671,
    ///         "diameterMeters": 1000
    ///     }
    ///
    /// </remarks>
    [HttpPost("nearby")]
    public async Task<IActionResult> GetNearby([FromBody] NearbyParkingRequest request)
    {
        if (request.DiameterMeters <= 0)
            return BadRequest("DiameterMeters must be greater than 0.");

        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return BadRequest("Invalid latitude or longitude.");

        double radius = request.DiameterMeters / 2.0;
        var parkings = await _overpass.GetNearbyParkingsAsync(request.Latitude, request.Longitude, radius);

        return Ok(new
        {
            count = parkings.Count,
            center = new { latitude = request.Latitude, longitude = request.Longitude },
            radiusMeters = radius,
            parkings
        });
    }

    /// <summary>
    /// GET sorğusu ilə yaxın parkingləri qaytarır (query string).
    /// </summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyGet(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double diameter)
    {
        if (diameter <= 0)
            return BadRequest("diameter must be greater than 0.");

        if (lat is < -90 or > 90 || lon is < -180 or > 180)
            return BadRequest("Invalid lat or lon.");

        double radius = diameter / 2.0;
        var parkings = await _overpass.GetNearbyParkingsAsync(lat, lon, radius);

        return Ok(new
        {
            count = parkings.Count,
            center = new { latitude = lat, longitude = lon },
            radiusMeters = radius,
            parkings
        });
    }
}
