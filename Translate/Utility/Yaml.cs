using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.ObjectGraphVisitors;
using YamlDotNet.Serialization.TypeInspectors;

namespace Translate.Utility;

public class Yaml
{
    public static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .WithTypeInspector(inner => new DefaultExcludingTypeInspector(inner))
           .Build();
    }

    public static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }
}

public class DefaultExcludingTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeInspector;
    private readonly Dictionary<Type, object> _defaultInstances = new Dictionary<Type, object>();

    public DefaultExcludingTypeInspector(ITypeInspector innerTypeInspector)
    {
        _innerTypeInspector = innerTypeInspector;
    }

    public override string GetEnumName(Type enumType, string name)
    {
       return _innerTypeInspector.GetEnumName(enumType, name);
    }

    public override string GetEnumValue(object enumValue)
    {
        return _innerTypeInspector.GetEnumValue(enumValue);
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = _innerTypeInspector.GetProperties(type, container);

        // Get or create default instance for comparison
        if (!_defaultInstances.TryGetValue(type, out var defaultInstance))
        {
            try
            {
                if (!type.IsAbstract && !type.IsInterface && HasDefaultConstructor(type))
                {
                    defaultInstance = Activator.CreateInstance(type);
                    _defaultInstances[type] = defaultInstance!;
                }
            }
            catch
            {
                // If we can't create a default instance, just pass through all properties
                return properties;
            }
        }

        if (defaultInstance == null)
        {
            return properties;
        }

        var filteredProperties = new List<IPropertyDescriptor>();

        foreach (var property in properties)
        {
            // If we have a default instance, check if the property value is different
            var defaultValue = property.Read(defaultInstance);
            var currentValue = property.Read(container!);

            // Only include properties with values different from default
            if (!Equals(currentValue.Value, defaultValue.Value))
            {
                filteredProperties.Add(property);
            }
        }

        return filteredProperties;
    }

    private bool HasDefaultConstructor(Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) != null;
    }
}