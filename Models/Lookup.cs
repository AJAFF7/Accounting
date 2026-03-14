using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("Lookups")]
public class Lookup : BaseEntity
{
    [StringLength(250), Required, Index(true, $"\"{nameof(IsDeleted)}\" = FALSE", "Type")] public string Name { get; set; }
    [StringLength(50), Required, Index] public string Type { get; set; }
    [Index] public long? Sort { get; set; }
    public Dictionary<string, string> LocalizedNames { get; set; } = [];
    public Dictionary<string, string> Fields { get; set; } = [];

    public T GetFieldValue<T>(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));
        }

        if (Fields == null)
        {
            return default(T);
        }

        if (!Fields.TryGetValue(fieldName, out var value))
        {
            return default(T);
        }

        if (string.IsNullOrEmpty(value))
        {
            return default(T);
        }

        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }

            // Handle special cases for common types
            if (targetType == typeof(string))
            {
                return (T)(object)value;
            }
            
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolResult))
                {
                    return (T)(object)boolResult;
                }
                // Handle common string representations of boolean
                var lowerValue = value.ToLowerInvariant();
                if (lowerValue == "1" || lowerValue == "yes" || lowerValue == "y")
                {
                    return (T)(object)true;
                }
                if (lowerValue == "0" || lowerValue == "no" || lowerValue == "n")
                {
                    return (T)(object)false;
                }
                throw new FormatException($"Cannot convert '{value}' to boolean.");
            }

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out var enumResult))
                {
                    return (T)enumResult;
                }
                throw new FormatException($"Cannot convert '{value}' to enum type {targetType.Name}.");
            }

            // Use Convert.ChangeType for other types
            return (T)Convert.ChangeType(value, targetType);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new InvalidOperationException($"Cannot convert field '{fieldName}' with value '{value}' to type {typeof(T).Name}.", ex);
        }
    }
}