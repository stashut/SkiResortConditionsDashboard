using Microsoft.AspNetCore.Mvc;

namespace SkiResort.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private const string UnitPreferenceKey = "UnitPreference";
    private const string RegionFilterKey = "RegionFilter";
    private const string LastViewedResortKey = "LastViewedResort";

    // Criterion 10: Read active user context stored in session
    [HttpGet]
    public ActionResult GetSettings()
    {
        var unitPreference = HttpContext.Session.GetString(UnitPreferenceKey) ?? "cm";
        var regionFilter = HttpContext.Session.GetString(RegionFilterKey) ?? "All";
        var lastViewedResort = HttpContext.Session.GetString(LastViewedResortKey);

        return Ok(new
        {
            UnitPreference = unitPreference,
            RegionFilter = regionFilter,
            LastViewedResort = lastViewedResort
        });
    }

    // Criterion 10: Update session-backed preferences
    [HttpPost]
    public IActionResult SaveSettings([FromBody] SaveSettingsRequest request)
    {
        HttpContext.Session.SetString(UnitPreferenceKey, request.UnitPreference ?? "cm");
        HttpContext.Session.SetString(RegionFilterKey, request.RegionFilter ?? "All");

        if (!string.IsNullOrWhiteSpace(request.LastViewedResort))
        {
            HttpContext.Session.SetString(LastViewedResortKey, request.LastViewedResort);
        }

        return NoContent();
    }

    public sealed class SaveSettingsRequest
    {
        public string? UnitPreference { get; set; }
        public string? RegionFilter { get; set; }
        public string? LastViewedResort { get; set; }
    }
}

