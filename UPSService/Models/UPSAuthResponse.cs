/// <summary>
/// Represents the response received from the UPS authentication service.
/// </summary>
public class UPSAuthResponse {
    /// <summary>
    /// Gets or sets the access token provided by the UPS authentication service.
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds for which the access token is valid.
    /// </summary>
    public int ExpiresIn { get; set; }
}