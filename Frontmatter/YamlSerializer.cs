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
	/// Reusable deserializer instance
	/// </summary>
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithDuplicateKeyChecking()
		.Build();

	/// <summary>
	/// Reusable serializer instance
	/// </summary>
	private static readonly ISerializer Serializer = new SerializerBuilder()
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
		// Compute a hash of the content for the cache key
		uint cacheKey = HashUtil.ComputeHash(input);

		// Try to get from cache first
		if (ParsedYamlCache.TryGetValue(cacheKey, out result!))
		{
			return true;
		}

		result = null;

		try
		{
			result = Deserializer.Deserialize<Dictionary<string, object>>(input);

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
		return Serializer.Serialize(input);
	}
}
