using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Xml.Linq;

/// <summary>
/// Controller for handling UPS-related API requests.
/// </summary>
[ApiController]
[Route("api/ups")]
[Authorize]
public class UPSController : ControllerBase {
    private readonly IUPSApiService _upsService;
    private readonly ILogger<UPSController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UPSController"/> class.
    /// </summary>
    /// <param name="upsService">Service for interacting with the UPS API.</param>
    /// <param name="logger">Logger for logging errors and information.</param>
    public UPSController(IUPSApiService upsService, ILogger<UPSController> logger) {
        _upsService = upsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves shipping rates from UPS based on the provided request.
    /// </summary>
    /// <param name="request">The rate request containing origin, destination, and weight details.</param>
    /// <returns>A JSON object containing ground and air shipping rates, or an error message if the request fails.</returns>
    [HttpPost("rates")]
    public async Task<IActionResult> GetRates([FromBody] RateRequest request) {
        try {
            var ratesXml = await _upsService.GetShippingRatesAsync(
                request.FromZip, 
                request.ToZip, 
                request.Weight
            );
            
            // Optional: Convert XML to JSON for easier Classic ASP parsing
            var xmlDoc = XDocument.Parse(ratesXml);
            var jsonResponse = new {
                GroundRate = xmlDoc.Descendants("RatedShipment")
                    .FirstOrDefault(x => x.Element("Service")?.Element("Code")?.Value == "03")
                    ?.Element("TotalCharges")?.Element("MonetaryValue")?.Value,
                AirRate = xmlDoc.Descendants("RatedShipment")
                    .FirstOrDefault(x => x.Element("Service")?.Element("Code")?.Value == "02")
                    ?.Element("TotalCharges")?.Element("MonetaryValue")?.Value
            };

            return Ok(jsonResponse);
        } catch (Exception ex) {
            _logger.LogError(ex, "UPS Rate Request Failed");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}