using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Patron
{
    // Core Identity
    public string Name { get; set; }
    public string Email { get; set; }
    public string Discord { get; set; }
    public string UserId { get; set; }
    
    // Patreon Status
    public string PatronStatus { get; set; }
    public bool FollowsYou { get; set; }
    public bool FreeMember { get; set; }
    public bool FreeTrial { get; set; }
    
    // Financial Information
    public decimal LifetimeAmount { get; set; }
    public decimal PledgeAmount { get; set; }
    public string ChargeFrequency { get; set; }
    public string Tier { get; set; }
    public string Currency { get; set; }
    
    // Address Information
    public string Addressee { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string Country { get; set; }
    public string FullCountryName { get; set; }
    public string Phone { get; set; }
    
    // Dates - Now nullable to handle missing values
    public DateTime? PatronageSinceDate { get; set; }
    public DateTime? LastChargeDate { get; set; }
    public DateTime? NextChargeDate { get; set; }
    public DateTime? AccessExpiration { get; set; }
    public DateTime? LastUpdated { get; set; }
    
    // Other Details
    public string LastChargeStatus { get; set; }
    public string AdditionalDetails { get; set; }
    public int MaxPosts { get; set; }
    public string SubscriptionSource { get; set; }
    
    // Computed Properties for easier querying
    public bool IsActive => 
        PatronStatus?.ToLower() == "active" || 
        PatronStatus?.ToLower() == "active patron";
    
    public bool HasValidPayment => 
        LastChargeStatus?.ToLower() == "paid" || 
        LastChargeStatus?.ToLower() == "success";
    
    public bool IsPaidTier => 
        !FreeMember && !FreeTrial && PledgeAmount > 0;
}

public partial class PatronManager
{
    public List<string> publicThanksTiers = new List<string>()
    {
        "Lil Blob",
        "Big blob",
        "Gigablob",
        "Public Thanks",
        "Personal phone call",
        "On-screen thanks"
    };
    
    public List<Patron> patrons = new List<Patron>();
    private string csvPath = "res://20251228-members-1433431-primerlearning.csv";
    
    private string excludedPath = "res://addons/PrimerAssets/Patreon/exclusions.txt";
    private HashSet<string> _excludedPatrons;
    private bool _exclusionListLoaded = false;

    public IReadOnlySet<string> ExcludedPatrons
    {
        get
        {
            if (!_exclusionListLoaded)
            {
                LoadExclusionList();
            }
            return _excludedPatrons;
        }
    }
    
    private string inclusionsPath = "res://addons/PrimerAssets/Patreon/inclusions.txt";
    private HashSet<string> _includedPatrons;
    private bool _inclusionListLoaded = false;

    public IReadOnlySet<string> IncludedPatrons
    {
        get
        {
            if (!_inclusionListLoaded)
            {
                LoadInclusionList();
            }
            return _includedPatrons;
        }
    }
    
    private string substitutionsPath = "res://addons/PrimerAssets/Patreon/substitutions.txt";
    private Dictionary<string, string> _nameSubstitutions;
    private bool _substitutionsLoaded = false;

    public IReadOnlyDictionary<string, string> NameSubstitutions
    {
        get
        {
            if (!_substitutionsLoaded)
            {
                LoadSubstitutions();
            }
            return _nameSubstitutions;
        }
    }

    public List<string> GetPatronsNamesForVideoThanks(DateTime lastVideoDate)
    {
        LoadPatronsFromCSV();
        ApplyNameSubstitutions();
        
        // Get patrons from public thanks tiers
        var eligiblePatrons = patrons
            .Where(x => publicThanksTiers.Contains(x.Tier))
            .ToList();
        
        // Manually include patrons from inclusions.txt
        var manuallyIncludedPatrons = patrons
            .Where(IsIncluded)
            .ToList();
        
        // Combine both lists
        eligiblePatrons.AddRange(manuallyIncludedPatrons);
        
        eligiblePatrons.Sort((x, y) => String.Compare(x.Name, y.Name));
        
        // Apply remaining filters to the combined list
        return eligiblePatrons
            .Where(x => x.LastChargeDate > lastVideoDate)
            .Where(x => !IsExcluded(x))
            .Select(x => x.Name)
            .Distinct() // Ensure no duplicates
            .ToList();
    }
    
    private void LoadInclusionList()
    {
        _includedPatrons = new HashSet<string>();
    
        if (FileAccess.FileExists(inclusionsPath))
        {
            using var file = FileAccess.Open(inclusionsPath, FileAccess.ModeFlags.Read);
            while (!file.EofReached())
            {
                string line = file.GetLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _includedPatrons.Add(line.ToLower());
                }
            }
            GD.Print($"Loaded {_includedPatrons.Count} manually included patrons");
        }
        else
        {
            GD.Print("No inclusion file found, using empty inclusion list");
        }
    
        _inclusionListLoaded = true;

        GetUnmatchedInclusions();
    }
    
    private void LoadExclusionList()
    {
        _excludedPatrons = new HashSet<string>();
    
        if (FileAccess.FileExists(excludedPath))
        {
            using var file = FileAccess.Open(excludedPath, FileAccess.ModeFlags.Read);
            while (!file.EofReached())
            {
                string line = file.GetLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _excludedPatrons.Add(line.ToLower());
                }
            }
            GD.Print($"Loaded {_excludedPatrons.Count} excluded patrons");
        }
        else
        {
            GD.Print("No exclusion file found, using empty exclusion list");
        }
    
        _exclusionListLoaded = true;

        GetUnmatchedExclusions();
    }
    
    private void LoadSubstitutions()
    {
        _nameSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
        if (FileAccess.FileExists(substitutionsPath))
        {
            using var file = FileAccess.Open(substitutionsPath, FileAccess.ModeFlags.Read);
            int lineNumber = 0;
        
            while (!file.EofReached())
            {
                lineNumber++;
                string line = file.GetLine().Trim();
            
                if (string.IsNullOrWhiteSpace(line))
                    continue;
            
                // Parse the line expecting format: "Original Name, Requested Name"
                var parts = line.Split(',');
            
                if (parts.Length >= 2)
                {
                    string originalName = parts[0].Trim();
                    string requestedName = string.Join(",", parts.Skip(1)).Trim(); // Handle cases where requested name might contain commas
                
                    if (!string.IsNullOrWhiteSpace(originalName) && !string.IsNullOrWhiteSpace(requestedName))
                    {
                        _nameSubstitutions[originalName] = requestedName;
                    }
                    else
                    {
                        GD.PrintErr($"Invalid substitution format on line {lineNumber}: '{line}'");
                    }
                }
                else
                {
                    GD.PrintErr($"Invalid substitution format on line {lineNumber}: '{line}' - expected 'Original Name, Requested Name'");
                }
            }
        
            GD.Print($"Loaded {_nameSubstitutions.Count} name substitutions");
        
            CheckUnmatchedSubstitutions();
        }
        else
        {
            GD.Print("No substitutions file found, using original names");
        }
    
        _substitutionsLoaded = true;
    }
    
    private void CheckUnmatchedSubstitutions()
    {
        var patronNames = new HashSet<string>(patrons.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        var unmatched = _nameSubstitutions.Keys.Where(name => !patronNames.Contains(name)).ToList();
    
        if (unmatched.Count > 0)
        {
            GD.PushWarning("Unmatched substitutions found:");
            foreach (var name in unmatched)
            {
                GD.Print($"'{name}' not found in patron data (requested: '{_nameSubstitutions[name]}')");
            }
        }
    }
    
    public void ApplyNameSubstitutions()
    {
        if (!_substitutionsLoaded)
        {
            LoadSubstitutions();
        }
    
        foreach (var patron in patrons)
        {
            if (_nameSubstitutions.TryGetValue(patron.Name, out string substituteName))
            {
                patron.Name = substituteName;
            }
        }
    }
    
    public void LoadPatronsFromCSV()
    {
        using var file = FileAccess.Open(csvPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Could not open CSV file at {csvPath}");
            return;
        }
        
        // Read header line to get column indices
        string headerLine = file.GetLine();
        var headers = ParseCSVLine(headerLine);
        
        // Create a dictionary to map column names to indices
        var columnIndex = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            columnIndex[headers[i].ToLower().Trim()] = i;
        }
        
        // Read data rows
        int rowNumber = 1; // Start at 1 since header is row 0
        while (!file.EofReached())
        {
            rowNumber++;
            string line = file.GetLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            var values = ParseCSVLine(line);
            if (values.Count == 0)
                continue;
            
            try
            {
                var patron = new Patron
                {
                    // Core Identity
                    Name = GetValue(values, columnIndex, "name"),
                    Email = GetValue(values, columnIndex, "email"),
                    Discord = GetValue(values, columnIndex, "discord"),
                    UserId = GetValue(values, columnIndex, "user id"),
                    
                    // Patreon Status
                    PatronStatus = GetValue(values, columnIndex, "patron status"),
                    FollowsYou = GetBoolValue(values, columnIndex, "follows you"),
                    FreeMember = GetBoolValue(values, columnIndex, "free member"),
                    FreeTrial = GetBoolValue(values, columnIndex, "free trial"),
                    
                    // Financial Information
                    LifetimeAmount = GetDecimalValue(values, columnIndex, "lifetime amount"),
                    PledgeAmount = GetDecimalValue(values, columnIndex, "pledge amount"),
                    ChargeFrequency = GetValue(values, columnIndex, "charge frequency"),
                    Tier = GetValue(values, columnIndex, "tier"),
                    Currency = GetValue(values, columnIndex, "currency"),
                    
                    // Address Information
                    Addressee = GetValue(values, columnIndex, "addressee"),
                    Street = GetValue(values, columnIndex, "street"),
                    City = GetValue(values, columnIndex, "city"),
                    State = GetValue(values, columnIndex, "state"),
                    Zip = GetValue(values, columnIndex, "zip"),
                    Country = GetValue(values, columnIndex, "country"),
                    FullCountryName = GetValue(values, columnIndex, "full country name"),
                    Phone = GetValue(values, columnIndex, "phone"),
                    
                    // Dates - Now using safe parsing method
                    PatronageSinceDate = GetDateTimeValue(values, columnIndex, "patronage since date"),
                    LastChargeDate = GetDateTimeValue(values, columnIndex, "last charge date"),
                    NextChargeDate = GetDateTimeValue(values, columnIndex, "next charge date"),
                    AccessExpiration = GetDateTimeValue(values, columnIndex, "access expiration"),
                    LastUpdated = GetDateTimeValue(values, columnIndex, "last updated"),
                    
                    // Other Details
                    LastChargeStatus = GetValue(values, columnIndex, "last charge status"),
                    AdditionalDetails = GetValue(values, columnIndex, "additional details"),
                    MaxPosts = GetIntValue(values, columnIndex, "max posts"),
                    SubscriptionSource = GetValue(values, columnIndex, "subscription source")
                };
                
                patrons.Add(patron);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error parsing row {rowNumber}: {e.Message}");
                // Optionally, you might want to skip this row and continue
                // Or you could add a partially filled patron with whatever data was successfully parsed
            }
        }
        
        GD.Print($"Loaded {patrons.Count} patrons from CSV");
    }
    
    // Helper method to parse a CSV line (handles quoted values with commas)
    private List<string> ParseCSVLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        string currentValue = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.Trim());
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }
        
        // Add the last value
        if (!string.IsNullOrEmpty(currentValue) || line.EndsWith(","))
        {
            values.Add(currentValue.Trim());
        }
        
        return values;
    }
    
    // Helper methods for getting typed values

    #region Value getters
    private string GetValue(List<string> values, Dictionary<string, int> columnIndex, params string[] possibleColumnNames)
    {
        foreach (var columnName in possibleColumnNames)
        {
            if (columnIndex.TryGetValue(columnName.ToLower().Trim(), out int index) && index < values.Count)
            {
                return values[index];
            }
        }
        return "";
    }
    
    private bool GetBoolValue(List<string> values, Dictionary<string, int> columnIndex, params string[] possibleColumnNames)
    {
        string value = GetValue(values, columnIndex, possibleColumnNames).ToLower();
        return value == "true" || value == "yes" || value == "1";
    }
    
    private decimal GetDecimalValue(List<string> values, Dictionary<string, int> columnIndex, params string[] possibleColumnNames)
    {
        string value = GetValue(values, columnIndex, possibleColumnNames);
        // Remove currency symbols and parse
        value = value.Replace("$", "").Replace("€", "").Replace("£", "").Replace(",", "").Trim();
        if (decimal.TryParse(value, out decimal result))
            return result;
        return 0;
    }
    
    private int GetIntValue(List<string> values, Dictionary<string, int> columnIndex, params string[] possibleColumnNames)
    {
        string value = GetValue(values, columnIndex, possibleColumnNames);
        if (int.TryParse(value, out int result))
            return result;
        return 0;
    }
    
    private DateTime? GetDateTimeValue(List<string> values, Dictionary<string, int> columnIndex, params string[] possibleColumnNames)
    {
        string value = GetValue(values, columnIndex, possibleColumnNames);
        
        // Check if the value is empty or null
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // Try to parse the date
        if (DateTime.TryParse(value, out DateTime result))
            return result;
        
        // If parsing fails, you might want to log it
        GD.PrintErr($"Could not parse date value: '{value}' for column: {string.Join("/", possibleColumnNames)}");
        return null;
    }
    #endregion
    
    public bool IsIncluded(Patron patron)
    {
        // Check if patron is manually included (by name, email, or discord)
        return IncludedPatrons.Contains(patron.Name?.ToLower()) || 
               IncludedPatrons.Contains(patron.Email?.ToLower()) ||
               IncludedPatrons.Contains(patron.Discord?.ToLower());
    }
    
    public bool IsExcluded(Patron patron)
    {
        // Check if patron requested exclusion (by name or email)
        return ExcludedPatrons.Contains(patron.Name?.ToLower()) || 
               ExcludedPatrons.Contains(patron.Email?.ToLower()) ||
               ExcludedPatrons.Contains(patron.Discord?.ToLower());
    }
    
    // Get list of all included names that weren't found in the patron list
    public void GetUnmatchedInclusions()
    {
        var unmatched = new List<string>();
        
        var patronIdentifiers = new HashSet<string>();
        foreach (var p in patrons)
        {
            if (!string.IsNullOrWhiteSpace(p.Name))
                patronIdentifiers.Add(p.Name.ToLower());
            if (!string.IsNullOrWhiteSpace(p.Email))
                patronIdentifiers.Add(p.Email.ToLower());
            if (!string.IsNullOrWhiteSpace(p.Discord))
                patronIdentifiers.Add(p.Discord.ToLower());
        }
        
        foreach (var included in IncludedPatrons)
        {
            if (!patronIdentifiers.Contains(included))
            {
                unmatched.Add(included);
            }
        }

        if (unmatched.Count > 0)
        {
            GD.PushWarning("Unmatched inclusions found:");
            foreach (var inclusion in unmatched)
            {
                GD.Print($"'{inclusion}' not found in patron data");
            }
        }
    }
    
    // Get list of all excluded names that weren't found in the patron list
    public void GetUnmatchedExclusions()
    {
        var unmatched = new List<string>();
        
        var patronIdentifiers = new HashSet<string>();
        foreach (var p in patrons)
        {
            if (!string.IsNullOrWhiteSpace(p.Name))
                patronIdentifiers.Add(p.Name.ToLower());
            if (!string.IsNullOrWhiteSpace(p.Email))
                patronIdentifiers.Add(p.Email.ToLower());
            if (!string.IsNullOrWhiteSpace(p.Discord))
                patronIdentifiers.Add(p.Discord.ToLower());
        }
        
        foreach (var excluded in ExcludedPatrons)
        {
            if (!patronIdentifiers.Contains(excluded))
            {
                unmatched.Add(excluded);
            }
        }

        if (unmatched.Count > 0)
        {
            GD.PushWarning("Unmatched exclusions found:");
            foreach (var exclusion in unmatched)
            {
                GD.Print($"'{exclusion}' not found in patron data");
            }
        }
    }
}