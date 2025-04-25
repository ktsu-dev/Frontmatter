namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

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
	/// Cache for serialized YAML to avoid repeated serialization
	/// </summary>
	private static readonly ConcurrentDictionary<uint, string> SerializedYamlCache = new();

	/// <summary>
	/// Attempts to parse a YAML string into a dictionary object.
	/// </summary>
	/// <param name="input">The YAML string to parse.</param>
	/// <param name="result">When this method returns, contains the deserialized dictionary if parsing succeeded, or null if parsing failed.</param>
	/// <returns>true if the YAML was successfully parsed; otherwise, false.</returns>
	public static bool TryParseYamlObject(string input, [NotNullWhen(true)] out Dictionary<string, object>? result)
	{
		// Compute a hash of the content for the cache key
		uint cacheKey = HashUtil.ComputeHash(input);

		// Try to get from cache first
		if (ParsedYamlCache.TryGetValue(cacheKey, out result!))
		{
			return true;
		}

		result = null;

		var deserializer = new DeserializerBuilder()
			.WithDuplicateKeyChecking()
			.Build();

		try
		{
			result = deserializer.Deserialize<Dictionary<string, object>>(input);

			// Cache the successfully parsed result
			if (result != null)
			{
				ParsedYamlCache.TryAdd(cacheKey, result);
			}
		}
		catch (YamlException) { }

		return result is not null;
	}

	/// <summary>
	/// Serializes a dictionary to a YAML string.
	/// </summary>
	/// <param name="input">The dictionary to serialize.</param>
	/// <returns>A string containing the serialized YAML.</returns>
	public static string SerializeYamlObject(Dictionary<string, object> input)
	{
		// Generate YAML content first to compute hash
		var serializer = new SerializerBuilder()
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
			.Build();

		string serialized = serializer.Serialize(input);

		// Compute hash for caching
		uint cacheKey = HashUtil.ComputeHash(serialized);

		// Try to get from cache first
		if (SerializedYamlCache.TryGetValue(cacheKey, out string? cachedResult))
		{
			return cachedResult;
		}

		// Cache the result
		SerializedYamlCache.TryAdd(cacheKey, serialized);
		return serialized;
	}
}
