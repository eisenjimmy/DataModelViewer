using System;

namespace SchemaDiagramViewer.Services;

public class ExportSettings
{
    public float WidthInches { get; set; } = 32f;
    public float HeightInches { get; set; } = 36f;
    public string DocumentName { get; set; } = "Data Model";
    public bool UseCustom { get; set; } = true;
    public string Preset { get; set; } = "CP Custom"; // "CP Custom" or "Letter" or "Custom"

    public void ApplyPreset(string preset)
    {
        Preset = preset ?? "CP Custom";
        UseCustom = preset == "Custom";
        if (preset == "Letter")
        {
            WidthInches = 8f;
            HeightInches = 11.5f;
            UseCustom = false;
        }
        else if (preset == "CP Custom")
        {
            WidthInches = 32f;
            HeightInches = 36f;
            UseCustom = false;
        }
        else // Custom
        {
            UseCustom = true;
        }
    }
}

public class ExportSettingsService
{
    public ExportSettings Settings { get; } = new ExportSettings();
}