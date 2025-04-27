namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Provides utilities for parsing and serializing YAML content.
/// </summary>
public static class YamlSerializer
{
	/// <summary>
	/// Cache for parsed YAML to avoid repeated parsing
	/// </summary>
	private static readonly ConcurrentDictionary<uint, Dictionary<string, object>> ParsedYamlCache = new();

	/// <summary>
	/// Reusable deserializer instance
	/// </summary>
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithNamingConvention(NullNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.Build();

	/// <summary>
	/// Reusable serializer instance
	/// </summary>
	private static readonly ISerializer Serializer = new SerializerBuilder()
		.WithNamingConvention(NullNamingConvention.Instance)
		.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
		.Build();

	/// <summary>
	/// Attempts to parse a YAML string into a dictionary object.
	/// </summary>
	/// <param name="input">The YAML string to parse.</param>
	/// <param name="result">When this method returns, contains the deserialized dictionary if parsing succeeded, or null if parsing failed.</param>
	/// <returns>true if the YAML was successfully parsed; otherwise, false.</returns>
	public static bool TryParseYamlObject(string input, [NotNullWhen(true)] out Dictionary<string, object>? result)
	{
		result = null;

		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}

		// Compute a hash of the content for the cache key
		uint cacheKey = HashUtil.ComputeHash(input);

		// Try to get from cache first
		if (ParsedYamlCache.TryGetValue(cacheKey, out var cachedResult))
		{
			// Create a deep copy of the cached dictionary to prevent mutations from affecting other copies
			result = [];
			foreach (var pair in cachedResult)
			{
				result[pair.Key] = DeepCloneValue(pair.Value);
			}

			return true;
		}

		try
		{
			// Simple approach with direct deserializer
			var rawData = Deserializer.Deserialize<Dictionary<object, object>>(input);
			result = [];

			// Convert dictionary keys to strings and preserve the first occurrence of duplicate keys
			foreach (var pair in rawData)
			{
				if (pair.Key != null)
				{
					string key = pair.Key.ToString()!;
					// Only add the key if it doesn't already exist
					if (!result.ContainsKey(key))
					{
						result[key] = pair.Value ?? string.Empty;
					}
				}
			}

			// Cache the successfully parsed result
			if (result.Count > 0)
			{
				// Create a deep copy for caching to prevent the cached instance from being modified
				var cacheResult = new Dictionary<string, object>();
				foreach (var pair in result)
				{
					cacheResult[pair.Key] = DeepCloneValue(pair.Value);
				}

				ParsedYamlCache.TryAdd(cacheKey, cacheResult);
				return true;
			}
		}
		catch (YamlException)
		{
			// Return false for any YAML parsing errors
			result = null;
		}
		catch (InvalidOperationException) // More specific exception for deserialization errors
		{
			// Return false for deserialization errors
			result = null;
		}
		catch (ArgumentException) // More specific exception for argument errors
		{
			// Return false for argument errors
			result = null;
		}

		return false;
	}

	/// <summary>
	/// Serializes a dictionary to a YAML string.
	/// </summary>
	/// <param name="input">The dictionary to serialize.</param>
	/// <returns>A string containing the serialized YAML.</returns>
	public static string SerializeYamlObject(Dictionary<string, object> input) =>
		input == null || input.Count == 0 ? string.Empty : Serializer.Serialize(input);

	/// <summary>
	/// Creates a deep clone of a value, handling nested dictionaries and collections.
	/// </summary>
	private static object DeepCloneValue(object value)
	{
		return value switch
		{
			Dictionary<string, object> dict => new Dictionary<string, object>(dict.Select(kvp =>
				new KeyValuePair<string, object>(kvp.Key, DeepCloneValue(kvp.Value)))),
			Dictionary<object, object> dict => new Dictionary<string, object>(dict.Select(kvp =>
				new KeyValuePair<string, object>(kvp.Key?.ToString() ?? string.Empty, DeepCloneValue(kvp.Value)))),
			IList<object> list => list.Select(DeepCloneValue).ToList(),
			System.Collections.IList list => list.Cast<object>().Select(DeepCloneValue).ToList(),
			_ => value // For primitive types and strings, which are immutable
		};
	}
}
