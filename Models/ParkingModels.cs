namespace ParkingApi.Models;

public class NearbyParkingRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>Diametr metrle. Yarıçap = Diametr / 2</summary>
    public double DiameterMeters { get; set; }
}

public class ParkingLocation
{
    public long Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Name { get; set; }
    public string? Access { get; set; }   // public, private, customers
    public string? Fee { get; set; }      // yes, no
    public double DistanceMeters { get; set; }
}
