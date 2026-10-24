namespace Zapp.Core.Utilities;

// estimate approximate physical distance based on rssi attenuation
public static class BleSignalEstimator
{
    // compute rough proximity in meters using path loss model
    public static double EstimateDistanceMeters(int rssi, int measuredPowerAtOneMeter = -59, double pathLossExponent = 2.0)
    {
        if (rssi == 0)
        {
            // return negative distance to indicate unknown proximity
            return -1.0;
        }

        // calculate ratio between measured power and observed rssi
        var ratio = (measuredPowerAtOneMeter - rssi) / (10.0 * pathLossExponent);

        // evaluate logarithmic distance approximation
        return Math.Pow(10, ratio);
    }

    // map raw rssi value into visual proximity band
    public static string ResolveProximityZone(int rssi) => rssi switch
    {
        >= -60 => "Immediate",
        >= -75 => "Near",
        >= -90 => "Far",
        _ => "Barely Detectable"
    };
}