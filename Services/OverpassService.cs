using System.Text.Json;
using ParkingApi.Models;

namespace ParkingApi.Services;

public class OverpassService
{
    private readonly HttpClient _http;

    // Fallback mirror siyahısı — biri 504 versə növbəti cəhd edilir
    private static readonly string[] Mirrors =
    [
        "https://overpass.kumi.systems/api/interpreter",
        "https://overpass-api.de/api/interpreter",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
        "https://overpass.openstreetmap.ru/api/interpreter"
    ];

    public OverpassService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<List<ParkingLocation>> GetNearbyParkingsAsync(double lat, double lon, double radiusMeters)
    {
        string query = $"""
            [out:json][timeout:15];
            (
              node["amenity"="parking"](around:{radiusMeters},{lat},{lon});
              way["amenity"="parking"](around:{radiusMeters},{lat},{lon});
            );
            out center;
            """;

        HttpResponseMessage? response = null;
        Exception? lastEx = null;

        foreach (var mirror in Mirrors)
        {
            try
            {
                var content = new FormUrlEncodedContent([new("data", query)]);
                response = await _http.PostAsync(mirror, content);
                if (response.IsSuccessStatusCode) break;
                lastEx = new HttpRequestException($"{mirror} returned {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                lastEx = ex;
                response = null;
            }
        }

        if (response is null || !response.IsSuccessStatusCode)
            throw lastEx ?? new HttpRequestException("All Overpass mirrors failed.");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var results = new List<ParkingLocation>();

        foreach (var element in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            double eLat, eLon;
            string type = element.GetProperty("type").GetString()!;

            if (type == "node")
            {
                eLat = element.GetProperty("lat").GetDouble();
                eLon = element.GetProperty("lon").GetDouble();
            }
            else if (type == "way" && element.TryGetProperty("center", out var center))
            {
                eLat = center.GetProperty("lat").GetDouble();
                eLon = center.GetProperty("lon").GetDouble();
            }
            else continue;

            var tags = element.TryGetProperty("tags", out var tagsEl) ? tagsEl : default;

            results.Add(new ParkingLocation
            {
                Id        = element.GetProperty("id").GetInt64(),
                Latitude  = eLat,
                Longitude = eLon,
                Name      = GetTag(tags, "name"),
                Access    = GetTag(tags, "access"),
                Fee       = GetTag(tags, "fee"),
                DistanceMeters = Math.Round(Haversine(lat, lon, eLat, eLon), 1)
            });
        }

        return results.OrderBy(p => p.DistanceMeters).ToList();
    }

    private static string? GetTag(JsonElement tags, string key)
    {
        if (tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty(key, out var val))
            return val.GetString();
        return null;
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
