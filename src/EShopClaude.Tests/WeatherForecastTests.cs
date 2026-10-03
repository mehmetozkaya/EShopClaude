using ApiForecast = global::WeatherForecast;
using WebForecast = EShopClaude.Web.WeatherForecast;

namespace EShopClaude.Tests;

public class WeatherForecastTests
{
    public static TheoryData<int, int> CelsiusToFahrenheit => new()
    {
        { 100, 212 },
        { 0, 32 },
        { 37, 99 },   // 98.6
        { -40, -40 },
        { -20, -4 },
        { 54, 129 },  // 129.2
        { -21, -6 },  // -5.8
    };

    [Theory]
    [MemberData(nameof(CelsiusToFahrenheit))]
    public void Web_TemperatureF_IsRoundedConversion(int celsius, int expectedFahrenheit)
    {
        var forecast = new WebForecast(DateOnly.MinValue, celsius, null);

        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }

    [Theory]
    [MemberData(nameof(CelsiusToFahrenheit))]
    public void Api_TemperatureF_IsRoundedConversion(int celsius, int expectedFahrenheit)
    {
        var forecast = new ApiForecast(DateOnly.MinValue, celsius, null);

        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }
}
