using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace GladiatorManager.addons.PrimerTools;

public class PrimerConfig
{
    // Just a color palette handler at this point
    private const string ConfigPath = "res://primer_config.json";
    public ColorPalette Colors { get; set; }

    public PrimerConfig()
    {
        Colors = new ColorPalette();
    }
    
    public class ColorPalette
    {
        public Color Blue { get; set; } = Godot.Colors.DodgerBlue;
        public Color Red { get; set; } = Godot.Colors.Crimson;
        public Color Green { get; set; } = Godot.Colors.ForestGreen;
        public Color Orange { get; set; } = Godot.Colors.DarkOrange;
        public Color Purple { get; set; } = Godot.Colors.DarkOrchid;
        public Color Yellow { get; set; } = Godot.Colors.Gold;
        public Color Gray { get; set; } = Godot.Colors.Gray;
        public Color LightGray { get; set; } = Godot.Colors.LightGray;
        public Color White { get; set; } = Godot.Colors.White;
        public Color Black { get; set; } = Godot.Colors.Black;
    }

    private static PrimerConfig _instance;
    public static PrimerConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                LoadConfig();
            }
            return _instance;
        }
    }

    private static void LoadConfig(string configPath = ConfigPath)
    {
        // It's convenient to comment this out when iterating on the palette
        // using the default colors above.
        // Otherwise, you have to delete the config file between changes. 
        // return new PrimerConfig();
        
        configPath = ProjectSettings.GlobalizePath(configPath);
        try
        {
            // Try to load custom config
            string jsonString = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            _instance = JsonSerializer.Deserialize<PrimerConfig>(jsonString, options);
        }
        catch (FileNotFoundException ex)
        {
            GD.PushWarning($"Config file not found at: {configPath}. Creating default config.", ex);
            _instance = new PrimerConfig();
            SaveConfig();
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error parsing JSON from file: {configPath}", ex);
        }
    }
    
    public static void SaveConfig(string configPath = ConfigPath)
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