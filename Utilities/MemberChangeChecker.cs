using System.Collections.Generic;
using System.Reflection;

public class MemberChangeChecker
{
    private readonly object _targetObject;
    private Dictionary<string, object> _previousMemberStates;

    public MemberChangeChecker(object targetObject)
    {
        _targetObject = targetObject;
        _previousMemberStates = new Dictionary<string, object>();
        InitializeMemberStates();
    }

    private void InitializeMemberStates()
    {
        // Initialize fields
        foreach (var field in _targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            _previousMemberStates["Field:" + field.Name] = field.GetValue(_targetObject);
        }
        
        // Initialize properties
        foreach (var prop in _targetObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.CanRead)
            {
                _previousMemberStates["Property:" + prop.Name] = prop.GetValue(_targetObject);
            }
        }
    }

    public bool CheckForChanges()
    {
        // Check fields
        foreach (var field in _targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var currentValue = field.GetValue(_targetObject);
            var previousValue = _previousMemberStates["Field:" + field.Name];

            if (!Equals(currentValue, previousValue))
            {
                _previousMemberStates["Field:" + field.Name] = currentValue;
                return true;
            }
        }

        // Check properties
        foreach (var prop in _targetObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.CanRead)
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
