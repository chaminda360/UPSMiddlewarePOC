/// <summary>
/// Represents a request for calculating shipping rates.
/// </summary>
public class RateRequest
{
    /// <summary>
    /// Gets or sets the origin ZIP code for the shipment.
    /// </summary>
    public string FromZip { get; set; }

    /// <summary>
    /// Gets or sets the destination ZIP code for the shipment.
    /// </summary>
    public string ToZip { get; set; }

    /// <summary>
    /// Gets or sets the weight of the shipment in decimal format.
/// </summary>
    public decimal Weight { get; set; }
}