using System;
using System.IO;
using System.Text.Json;
using Godot;
using FileAccess = System.IO.FileAccess;

namespace GladiatorManager.addons.PrimerTools;

public class PrimerConfig
{
    public ColorPalette Colors { get; set; }
    public AssetPaths Assets { get; set; }

    public PrimerConfig()
    {
        Colors = new ColorPalette();
    }
    
    public class ColorPalette
    {
        public Color Blue { get; set; } = Godot.Colors.Blue;
        public Color Red { get; set; } = Godot.Colors.Red;
        public Color Green { get; set; } = Godot.Colors.Green;
        public Color Orange { get; set; } = Godot.Colors.Orange;
        public Color Purple { get; set; } = Godot.Colors.Purple;
        public Color Yellow { get; set; } = Godot.Colors.Yellow;
        public Color Gray { get; set; } = Godot.Colors.Gray;
        public Color LightGray { get; set; } = Godot.Colors.LightGray;
        public Color White { get; set; } = Godot.Colors.White;
        public Color Black { get; set; } = Godot.Colors.Black;
    }

    public class AssetPaths
    {
        public string Models { get; set; } = "res://default_models";
        public string Materials { get; set; } = "res://default_materials";
        // etc
    }

    private static PrimerConfig _instance;
    public static PrimerConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = LoadConfig();
            }
            return _instance;
        }
    }

    private const string DefaultPath = "res://primer_config.json";

    private static PrimerConfig LoadConfig(string configPath = DefaultPath)
    {
        configPath = ProjectSettings.GlobalizePath(configPath);
        try
        {
            // Try to load custom config
            string jsonString = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<PrimerConfig>(jsonString, options);
        }
        catch (FileNotFoundException ex)
        {
            GD.PushWarning($"Config file not found at: {configPath}. Returning default config.", ex);
            return new PrimerConfig();
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error parsing JSON from file: {configPath}", ex);
        }
    }
    
    public static void SaveConfig(string configPath = DefaultPath)
    {
        configPath = ProjectSettings.GlobalizePath(configPath);
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(Instance, options);
            File.WriteAllText(configPath, jsonString);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save config to {configPath}: {ex.Message}", ex);
        }
    }
}