namespace Calcio.Components.Pages;

/// <summary>
/// Demonstrates streaming rendering by loading a short weather forecast list asynchronously.
/// </summary>
public partial class Weather
{
    /// <summary>
    /// Stores the generated weather forecast items for rendering.
    /// </summary>
    private WeatherForecast[]? forecasts;

    /// <summary>
    /// Loads sample weather data after a short delay to demonstrate asynchronous rendering.
    /// </summary>
    /// <returns>A task that completes when the sample data has been generated.</returns>
    protected override async Task OnInitializedAsync()
    {
        // Simulate asynchronous loading to demonstrate streaming rendering
        await Task.Delay(500);

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        forecasts = [.. Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })];
    }

    /// <summary>
    /// Represents a single weather forecast data point.
    /// </summary>
    private class WeatherForecast
    {
        /// <summary>
        /// Gets or sets the forecast date.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Gets or sets the forecast temperature in Celsius.
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Gets or sets the textual weather summary.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets the forecast temperature in Fahrenheit.
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
