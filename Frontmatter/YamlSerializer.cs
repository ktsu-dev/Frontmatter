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
	private sealed class LastValueNodeDeserializer(INodeDeserializer nodeDeserializer) : INodeDeserializer
	{
		public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
		{
			if (parser.Accept<MappingStart>(out _))
			{
				var dictionary = new Dictionary<string, object?>();
				parser.Consume<MappingStart>();

				while (!parser.Accept<MappingEnd>(out _))
				{
					string key = parser.Consume<Scalar>().Value;
					if (nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out object? val, rootDeserializer))
					{
						dictionary[key] = val;
					}
				}

				parser.Consume<MappingEnd>();
				value = dictionary;
				return true;
			}

			return nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
		}
	}

	/// <summary>
	/// Reusable deserializer instance
	/// </summary>
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithNodeDeserializer(inner => new LastValueNodeDeserializer(inner), s => s.InsteadOf<DictionaryNodeDeserializer>())
		.IgnoreUnmatchedProperties()
		.WithAttemptingUnquotedStringTypeDeserialization()
		.Build();

	/// <summary>
	/// Reusable serializer instance
	/// </summary>
	private static readonly ISerializer Serializer = new SerializerBuilder()
		.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
		.WithQuotingNecessaryStrings()
		.WithTypeConverter(new DateTimeConverter())
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

	/// <summary>
	/// Custom type converter for DateTime values
	/// </summary>
	private class DateTimeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) => type == typeof(DateTime);

		public object? ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
		{
			var scalar = parser.Consume<Scalar>();
			return DateTime.TryParse(scalar.Value, out var result) ? result : null;
		}

		public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
		{
			if (value is DateTime dateTime)
			{
				emitter.Emit(new Scalar(null, null, dateTime.ToString("O"), ScalarStyle.Any, true, false));
			}
		}
	}
}
