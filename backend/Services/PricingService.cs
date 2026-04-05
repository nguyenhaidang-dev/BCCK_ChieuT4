using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace backend.Services;

public class PricingService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly string _osrmBaseUrl;

    public PricingService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _httpClient = new HttpClient();
        _osrmBaseUrl = configuration.GetValue<string>("RoutingService:OsrmBaseUrl") ?? "http://router.project-osrm.org";
    }

    public async Task<decimal> CalculateDistanceAsync(decimal startLat, decimal startLng, decimal endLat, decimal endLng)
    {
        try
        {
            // OSRM API endpoint for routing
            var url = $"{_osrmBaseUrl}/route/v1/driving/{startLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{endLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var osrmResponse = JsonSerializer.Deserialize<OsrmResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (osrmResponse?.Routes?.Any() == true)
            {
                // OSRM returns distance in meters, convert to kilometers
                var distanceInMeters = osrmResponse.Routes[0].Distance;
                return (decimal)(distanceInMeters / 1000.0);
            }

            // Fallback to Haversine formula if OSRM fails
            return CalculateHaversineDistance(startLat, startLng, endLat, endLng);
        }
        catch (Exception)
        {
            // Fallback to Haversine formula if API call fails
            return CalculateHaversineDistance(startLat, startLng, endLat, endLng);
        }
    }

    public async Task<decimal> CalculateEstimatedPrice(decimal distanceKm, string vehicleType, decimal weight = 0)
    {
        // Get pricing parameters from system config (you can add these to SystemConfig table)
        // For now, using default values
        decimal baseFee = 50000; // VND
        decimal pricePerKm = 15000; // VND per km
        decimal waitingFee = 10000; // VND

        // Get vehicle type factor
        var vehicleFactor = await GetVehicleTypeFactor(vehicleType);

        // Calculate price: base_fee + (distance_km * price_per_km * vehicle_factor) + waiting_fee
        decimal price = baseFee + (distanceKm * pricePerKm * vehicleFactor) + waitingFee;

        // Apply minimum fare
        decimal minFare = 100000; // VND
        price = Math.Max(price, minFare);

        // Round to nearest 1000 VND
        price = Math.Round(price / 1000) * 1000;

        return price;
    }

    public async Task<TimeSpan> CalculateETAAsync(decimal startLat, decimal startLng, decimal endLat, decimal endLng)
    {
        try
        {
            var url = $"{_osrmBaseUrl}/route/v1/driving/{startLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{endLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var osrmResponse = JsonSerializer.Deserialize<OsrmResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (osrmResponse?.Routes?.Any() == true)
            {
                var durationInSeconds = osrmResponse.Routes[0].Duration;
                return TimeSpan.FromSeconds(durationInSeconds);
            }

            // Fallback: assume 30 km/h average speed
            var distance = CalculateHaversineDistance(startLat, startLng, endLat, endLng);
            return TimeSpan.FromHours((double)(distance / 30m));
        }
        catch (Exception)
        {
            // Fallback: assume 30 km/h average speed
            var distance = CalculateHaversineDistance(startLat, startLng, endLat, endLng);
            return TimeSpan.FromHours((double)(distance / 30m));
        }
    }

    public decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        return CalculateHaversineDistance(lat1, lon1, lat2, lon2);
    }

    private decimal CalculateHaversineDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const decimal R = 6371; // Earth's radius in km
        var dLat = (lat2 - lat1) * (decimal)(Math.PI / 180);
        var dLon = (lon2 - lon1) * (decimal)(Math.PI / 180);

        var a = (decimal)Math.Sin((double)dLat / 2) * (decimal)Math.Sin((double)dLat / 2) +
                (decimal)Math.Cos((double)lat1 * Math.PI / 180) * (decimal)Math.Cos((double)lat2 * Math.PI / 180) *
                (decimal)Math.Sin((double)dLon / 2) * (decimal)Math.Sin((double)dLon / 2);

        var c = 2 * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));

        return R * c;
    }

    private async Task<decimal> GetVehicleTypeFactor(string vehicleType)
    {
        var factor = await _context.VehicleTypeFactors
            .FirstOrDefaultAsync(v => v.VehicleType == vehicleType);

        return factor?.Factor ?? 1.0m; // Default factor of 1.0
    }
}

// OSRM API Response Models
public class OsrmResponse
{
    public string Code { get; set; } = string.Empty;
    public List<OsrmRoute> Routes { get; set; } = new();
    public List<object> Waypoints { get; set; } = new();
}

public class OsrmRoute
{
    public double Distance { get; set; } // in meters
    public double Duration { get; set; } // in seconds
    public string Geometry { get; set; } = string.Empty;
    public List<OsrmLeg> Legs { get; set; } = new();
}

public class OsrmLeg
{
    public double Distance { get; set; }
    public double Duration { get; set; }
    public List<object> Steps { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<object> Annotations { get; set; } = new();
}