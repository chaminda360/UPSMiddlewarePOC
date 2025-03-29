// Services/UPSApiService.cs
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public interface IUPSApiService {
    Task<string> GetShippingRatesAsync(string fromZip, string toZip, decimal weight);
}

public class UPSApiService : IUPSApiService {
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<UPSApiService> _logger;
    private readonly IMemoryCache _cache;

    public UPSApiService(IHttpClientFactory clientFactory, IConfiguration config, ILogger<UPSApiService> logger, IMemoryCache cache) {
        _clientFactory = clientFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    private async Task<string> GetOrRefreshTokenAsync() {
        if (_cache.TryGetValue("UPSAccessToken", out string cachedToken)) {
            return cachedToken;
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
                _logger.LogError("UPS Auth Failed: {Error}", await response.Content.ReadAsStringAsync());
                throw new Exception("UPS Auth Failed");
            }

            var tokenData = await response.Content.ReadFromJsonAsync<UPSAuthResponse>();
            _cache.Set("UPSAccessToken", tokenData.AccessToken, TimeSpan.FromSeconds(tokenData.ExpiresIn - 300));
            return tokenData.AccessToken;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error while fetching UPS access token");
            throw;
        }
    }

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
                _logger.LogError("UPS API Error: {StatusCode}", response.StatusCode);
                throw new Exception("UPS API Error");
            }

            var responseXml = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Serialize(XDocument.Parse(responseXml).Root);
            return responseJson;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error while fetching shipping rates");
            throw;
        }
    }
}