namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

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
	/// Custom node deserializer that keeps the last value when encountering duplicates
	/// </summary>
	private class LastValueNodeDeserializer(INodeDeserializer nodeDeserializer) : INodeDeserializer
	{
		private readonly INodeDeserializer _nodeDeserializer = nodeDeserializer;

		public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
		{
			if (parser.Current is MappingStart)
			{
				var mapping = new Dictionary<string, object>();
				parser.MoveNext();

				while (parser.Current is not MappingEnd)
				{
					if (parser.Current is not Scalar keyScalar)
					{
						throw new YamlException("Invalid YAML: Expected scalar key");
					}

					string key = keyScalar.Value;
					parser.MoveNext();

					if (_nodeDeserializer.Deserialize(parser, typeof(object), nestedObjectDeserializer, out object? valueObj, rootDeserializer))
					{
						// Always update with the latest value
						mapping[key] = valueObj!;
					}

					parser.MoveNext();
				}

				parser.MoveNext();
				value = mapping;
				return true;
			}

			return _nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
		}
	}

	/// <summary>
	/// Reusable deserializer instance
	/// </summary>
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithNodeDeserializer(inner => new LastValueNodeDeserializer(inner), s => s.InsteadOf<DictionaryNodeDeserializer>())
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
		result = null;

		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}

		// Compute a hash of the content for the cache key
		uint cacheKey = HashUtil.ComputeHash(input);

		// Try to get from cache first
		if (ParsedYamlCache.TryGetValue(cacheKey, out result!))
		{
			return true;
		}

		try
		{
			result = Deserializer.Deserialize<Dictionary<string, object>>(input);

			// Cache the successfully parsed result
			if (result != null)
			{
				ParsedYamlCache.TryAdd(cacheKey, result);
				return true;
			}
		}
		catch (YamlException)
		{
			// Return false for any YAML parsing errors
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
}
