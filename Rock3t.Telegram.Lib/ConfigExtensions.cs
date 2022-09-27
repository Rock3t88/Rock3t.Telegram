using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Rock3t.Telegram.Lib;

public static class ConfigExtensions
{
    public static void LogConfiguration(this ILogger logger, BotConfig config)
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<string> values = new List<string>();

        var settings = config.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("######################### Configuration Start #########################");
        stringBuilder.AppendLine("");

        foreach (PropertyInfo setting in settings)
        {
            if (setting.PropertyType.IsArray || setting.PropertyType.IsList())
            {
                stringBuilder.AppendArray(setting, values, config);
                continue;
            }

            string value = setting.GetValue(config)?.ToString() ?? "null";

            stringBuilder.AppendLine($"{setting.Name}: {value}");
            values.Add(value);
        }

        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("######################### Configuration End #########################");

        logger.LogInformation(stringBuilder.ToString());
    }

    private static void AppendArray(this StringBuilder stringBuilder, PropertyInfo property, List<string> values, BotConfig config)
    {
        IEnumerable? array = property.GetValue(config) as IEnumerable;

        if (array is null)
        {
            throw new InvalidOperationException(
                $"Could not cast {property.Name} of type {property.PropertyType} to IEnumerable.");
        }

        stringBuilder.AppendLine($"{property.Name}:");
        stringBuilder.AppendLine("[");

        foreach (var value in array)
        {
            string? strValue = value?.ToString();

            //if (strValue != null && strValue.Contains("{"))
            //{
            //    strValue = value?.ToString()?.Replace("{", "{{").Replace("}", "}}") ?? "null";
            //}

            stringBuilder.AppendLine($"\t\"{strValue}");
            values.Add($"{strValue}");
        }

        stringBuilder.AppendLine("]");
    }


    // https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect
    /// <summary>
    /// Indicates whether or not the specified type is a list.
    /// </summary>
    /// <param name="type">The type to query</param>
    /// <returns>True if the type is a list, otherwise false</returns>
    public static bool IsList(this Type type)
    {
        if (null == type)
            throw new ArgumentNullException("type");

        if (typeof(System.Collections.IList).IsAssignableFrom(type))
            return true;
        foreach (var it in type.GetInterfaces())
            if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition())
                return true;
        return false;
    }

    // https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect
    /// <summary>
    /// Retrieves the collection element type from this type
    /// </summary>
    /// <param name="type">The type to query</param>
    /// <returns>The element type of the collection or null if the type was not a collection
    /// </returns>
    public static Type GetCollectionElementType(Type type)
    {
        if (null == type)
            throw new ArgumentNullException("type");

        // first try the generic way
        // this is easy, just query the IEnumerable<T> interface for its generic parameter
        var etype = typeof(IEnumerable<>);
        foreach (var bt in type.GetInterfaces())
            if (bt.IsGenericType && bt.GetGenericTypeDefinition() == etype)
                return bt.GetGenericArguments()[0];

        // now try the non-generic way

        // if it's a dictionary we always return DictionaryEntry
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
            return typeof(System.Collections.DictionaryEntry);

        // if it's a list we look for an Item property with an int index parameter
        // where the property type is anything but object
        if (typeof(System.Collections.IList).IsAssignableFrom(type))
        {
            foreach (var prop in type.GetProperties())
            {
                if ("Item" == prop.Name && typeof(object) != prop.PropertyType)
                {
                    var ipa = prop.GetIndexParameters();
                    if (1 == ipa.Length && typeof(int) == ipa[0].ParameterType)
                    {
                        return prop.PropertyType;
                    }
                }
            }
        }

        // if it's a collection, we look for an Add() method whose parameter is 
        // anything but object
        if (typeof(System.Collections.ICollection).IsAssignableFrom(type))
        {
            foreach (var meth in type.GetMethods())
            {
                if ("Add" == meth.Name)
                {
                    var pa = meth.GetParameters();
                    if (1 == pa.Length && typeof(object) != pa[0].ParameterType)
                        return pa[0].ParameterType;
                }
            }
        }
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return typeof(object);
        return null;
    }
}