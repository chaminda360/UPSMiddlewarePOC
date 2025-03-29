// Controllers/UPSController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Xml.Linq;

[ApiController]
[Route("api/ups")]
[Authorize] // Require API key (see Step 5)
public class UPSController : ControllerBase {
    private readonly IUPSApiService _upsService;
    private readonly ILogger<UPSController> _logger;

    public UPSController(IUPSApiService upsService, ILogger<UPSController> logger) {
        _upsService = upsService;
        _logger = logger;
    }

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