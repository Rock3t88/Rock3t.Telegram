using System.Reflection;
using System.Text.RegularExpressions;

namespace Rock3t.Telegram.Bots.ScaryTerry;

public class Helper
{
    public List<object> TokenSource { get; set; } = new();
    public Dictionary<string, object?> Tokens { get; set; } = new();

    public object? GetTokenValue(string token)
    {
        if (Tokens.ContainsKey(token.ToLower()))
            return Tokens[token.ToLower()];
        else return null;

    }


    public string ReplaceTokens(string input, Dictionary<string, object?>? dict)
    {
        string output = input;

        foreach (KeyValuePair<string, object?> pair in Tokens)
        {
            string value = pair.Value?.ToString() ?? "";
            output = Regex.Replace(output, $"{{{pair.Key}}}", value, RegexOptions.IgnoreCase);
        }
        foreach (KeyValuePair<string, object?> pair in dict)
        {
            string value = pair.Value?.ToString() ?? "";
            output = Regex.Replace(output, $"{{{pair.Key}}}", value, RegexOptions.IgnoreCase);
        }

        return output;
    }

    public void AddToken(string key, object? value)
    {
        if (!Tokens.ContainsKey(key.ToLower()))
            Tokens.Add(key.ToLower(), value);
    }

    public void Init()
    {
        foreach (object obj in TokenSource)
        {
            var tokens = GetTokens(obj);

            foreach (KeyValuePair<string, object?> pair in tokens)
            {
                if (!Tokens.ContainsKey(pair.Key))
                {
                    Tokens.Add(pair.Key.ToLower(), pair.Value);
                }
            }
        }
    }

    public Dictionary<string, object?> GetTokens(object obj)
    {
        var dict = new Dictionary<string, object?>();

        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (IsSimple(property.PropertyType))
            {
                dict.Add(property.Name, property.GetValue(obj));
            }
        }

        return dict;
    }

    bool IsSimple(Type type)
    {
        var typeInfo = type.GetTypeInfo();
        if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // nullable type, check if the nested type is simple.
            return IsSimple(typeInfo.GetGenericArguments()[0]);
        }
        return typeInfo.IsPrimitive
               || typeInfo.IsEnum
               || type.Equals(typeof(string))
               || type.Equals(typeof(decimal));
    }
}