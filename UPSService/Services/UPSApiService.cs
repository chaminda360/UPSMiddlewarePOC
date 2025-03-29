using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Interface for UPS API Service.
/// Provides methods to interact with UPS API for fetching shipping rates.
/// </summary>
public interface IUPSApiService {
    /// <summary>
    /// Fetches shipping rates from UPS API based on the provided parameters.
    /// </summary>
    /// <param name="fromZip">The origin postal code.</param>
    /// <param name="toZip">The destination postal code.</param>
    /// <param name="weight">The weight of the package in pounds.</param>
    /// <returns>A JSON string containing the shipping rates.</returns>
    Task<string> GetShippingRatesAsync(string fromZip, string toZip, decimal weight);
}

/// <summary>
/// Implementation of the UPS API Service.
/// Handles authentication and communication with the UPS API.
/// </summary>
public class UPSApiService : IUPSApiService {
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<UPSApiService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UPSApiService"/> class.
    /// </summary>
    /// <param name="clientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="config">The configuration for accessing app settings.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="cache">The memory cache for caching data.</param>
    public UPSApiService(IHttpClientFactory clientFactory, IConfiguration config, ILogger<UPSApiService> logger, IMemoryCache cache) {
        _clientFactory = clientFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Retrieves or refreshes the UPS access token.
    /// Implements OAuth 2.0 token management for UPS API.
    /// </summary>
    private async Task<string> GetOrRefreshTokenAsync() {
        if (_cache.TryGetValue("UPSAccessToken", out string cachedToken)) {
            return cachedToken; // Caching: Retrieves token from memory cache if available.
        }

        try {
            var client = _clientFactory.CreateClient();
            var authHeader = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(
                    $"{_config["UPS:ClientId"]}:{_config["UPS:ClientSecret"]}"
                ))
            );

            client.DefaultRequestHeaders.Authorization = authHeader;
            var response = await client.PostAsync("https://onlinetools.ups.com/security/v1/oauth/token", 
                new FormUrlEncodedContent(new Dictionary<string, string> { { "grant_type", "client_credentials" } }));

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("UPS Auth Failed: {Error}", await response.Content.ReadAsStringAsync()); // Error logging: Logs authentication failure.
                throw new Exception("UPS Auth Failed");
            }

            var tokenData = await response.Content.ReadFromJsonAsync<UPSAuthResponse>();
            _cache.Set("UPSAccessToken", tokenData.AccessToken, TimeSpan.FromSeconds(tokenData.ExpiresIn - 300)); // Caching: Stores token in memory cache.
            return tokenData.AccessToken;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error while fetching UPS access token"); // Error logging: Logs exceptions during token retrieval.
            throw;
        }
    }

    /// <summary>
    /// Fetches shipping rates from UPS API.
    /// Converts XML response to JSON for legacy compatibility.
    /// </summary>
    public async Task<string> GetShippingRatesAsync(string fromZip, string toZip, decimal weight) {
        try {
            var token = await GetOrRefreshTokenAsync();
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var requestXml = new XDocument(
                new XElement("RatingServiceSelectionRequest",
                    new XElement("Request",
                        new XElement("RequestAction", "Rate"),
                        new XElement("RequestOption", "Shop")
                    ),
                    new XElement("Shipment",
                        new XElement("Shipper",
                            new XElement("Address", new XElement("PostalCode", fromZip))
                        ),
                        new XElement("ShipTo",
                            new XElement("Address", new XElement("PostalCode", toZip))
                        ),
                        new XElement("Package",
                            new XElement("PackageWeight",
                                new XElement("UnitOfMeasurement", new XElement("Code", "LBS")),
                                new XElement("Weight", weight)
                            )
                        )
                    )
                )
            );

            var response = await client.PostAsync("https://onlinetools.ups.com/api/rating/v2205",
                new StringContent(requestXml.ToString(), Encoding.UTF8, "application/xml"));

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("UPS API Error: {StatusCode}", response.StatusCode); // Error logging: Logs API errors.
                throw new Exception("UPS API Error");
            }

            var responseXml = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Serialize(XDocument.Parse(responseXml).Root); // XML-to-JSON conversion: Converts XML response to JSON.
            return responseJson;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error while fetching shipping rates"); // Error logging: Logs exceptions during rate fetching.
            throw;
        }
    }
}