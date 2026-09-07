using System.ComponentModel.DataAnnotations;
namespace LCAP.HRMS.Application.Attendance;

public interface IGeoDistanceService
{
    double DistanceMeters(double officeLatitude, double officeLongitude, double latitude, double longitude);
}
public sealed class GeoDistanceService : IGeoDistanceService
{
    public const double EarthRadiusMeters = 6371008.8;
    public double DistanceMeters(double officeLatitude, double officeLongitude, double latitude, double longitude)
    {
        if (!double.IsFinite(officeLatitude) || !double.IsFinite(officeLongitude) || !double.IsFinite(latitude) || !double.IsFinite(longitude)
            || Math.Abs(officeLatitude) > 90 || Math.Abs(latitude) > 90 || Math.Abs(officeLongitude) > 180 || Math.Abs(longitude) > 180)
            throw new ValidationException("Invalid geographic coordinates.");
        static double Radians(double degrees) => degrees * Math.PI / 180;
        var dLat = Radians(latitude - officeLatitude);
        var dLon = Radians(longitude - officeLongitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Radians(officeLatitude)) * Math.Cos(Radians(latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusMeters * Math.Atan2(Math.Sqrt(Math.Clamp(a, 0, 1)), Math.Sqrt(1 - Math.Clamp(a, 0, 1)));
    }
}
