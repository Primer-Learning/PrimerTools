using System.Collections.Generic;
using System.Reflection;
using Godot;

public class ExportedMemberChangeChecker
{
    private readonly object _targetObject;
    private Dictionary<string, object> _previousMemberStates;

    public ExportedMemberChangeChecker(object targetObject)
    {
        _targetObject = targetObject;
        _previousMemberStates = new Dictionary<string, object>();
        InitializeMemberStates();
    }

    private void InitializeMemberStates()
    {
        var type = _targetObject.GetType();
        
        // Initialize fields with [Export] attribute
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.GetCustomAttribute<ExportAttribute>() != null)
            {
                _previousMemberStates["Field:" + field.Name] = field.GetValue(_targetObject);
            }
        }
        
        // Initialize properties with [Export] attribute
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.CanRead && prop.GetCustomAttribute<ExportAttribute>() != null)
            {
                _previousMemberStates["Property:" + prop.Name] = prop.GetValue(_targetObject);
            }
        }
    }

    public bool CheckForChanges()
    {
        var type = _targetObject.GetType();

        // Check fields with [Export] attribute
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.GetCustomAttribute<ExportAttribute>() != null)
            {
                var currentValue = field.GetValue(_targetObject);
                var previousValue = _previousMemberStates["Field:" + field.Name];

                if (!Equals(currentValue, previousValue))
                {
                    _previousMemberStates["Field:" + field.Name] = currentValue;
                    return true;
                }
            }
        }

        // Check properties with [Export] attribute
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.CanRead && prop.GetCustomAttribute<ExportAttribute>() != null)
            {
                var currentValue = prop.GetValue(_targetObject);
                var previousValue = _previousMemberStates.ContainsKey("Property:" + prop.Name) ? _previousMemberStates["Property:" + prop.Name] : null;

                if (!Equals(currentValue, previousValue))
                {
                    _previousMemberStates["Property:" + prop.Name] = currentValue;
                    return true;
                }
            }
        }

        return false;
    }
}
