// Copyright (c) 2023-2026 ktsu-dev contributors

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
	/// Cache for parsed YAML to avoid repeated parsing.
	/// Keyed by the YAML text itself, compared ordinally, so a cache hit means the input really is
	/// identical rather than merely hashing alike.
	/// </summary>
	private static readonly ConcurrentDictionary<string, Dictionary<string, object>> ParsedYamlCache = new(StringComparer.Ordinal);

	/// <summary>
	/// The most values one block may expand to once its aliases are resolved. Frontmatter is a few dozen
	/// values; the limit only stops nested aliases from expanding exponentially.
	/// </summary>
	internal const int MaxExpandedValues = 100_000;

	/// <summary>
	/// Reusable deserializer instance
	/// </summary>
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithNamingConvention(NullNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.WithAttemptingUnquotedStringTypeDeserialization()
		.Build();

	/// <summary>
	/// Reusable serializer instance
	/// </summary>
	private static readonly ISerializer Serializer = new SerializerBuilder()
		.WithNamingConvention(NullNamingConvention.Instance)
		.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
		.WithQuotingNecessaryStrings()
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

		// Try to get from cache first
		if (ParsedYamlCache.TryGetValue(input, out Dictionary<string, object>? cachedResult))
		{
			// Create a deep copy of the cached dictionary to prevent mutations from affecting other copies
			result = [];
			foreach (KeyValuePair<string, object> pair in cachedResult)
			{
				result[pair.Key] = DeepCloneValue(pair.Value);
			}

			return true;
		}

		try
		{
			// Simple approach with direct deserializer
			// A document with no content (a comment only, ~ or null) deserializes to null
			Dictionary<object, object>? rawData = Deserializer.Deserialize<Dictionary<object, object>?>(input);
			if (rawData is null)
			{
				return false;
			}

			result = [];
			ConversionState state = new();

			// Convert dictionary keys to strings and preserve the first occurrence of duplicate keys
			foreach (KeyValuePair<object, object> pair in rawData)
			{
				if (pair.Key != null)
				{
					string key = pair.Key.ToString()!;
					// Only add the key if it doesn't already exist
					if (!result.ContainsKey(key))
					{
						// Convert the value to ensure proper type handling
						result[key] = ConvertValue(pair.Value, state);
					}
				}
			}

			// Cache the successfully parsed result
			if (result.Count > 0)
			{
				// Create a deep copy for caching to prevent the cached instance from being modified
				Dictionary<string, object> cacheResult = [];
				foreach (KeyValuePair<string, object> pair in result)
				{
					cacheResult[pair.Key] = DeepCloneValue(pair.Value);
				}

				ParsedYamlCache.TryAdd(input, cacheResult);
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
			Dictionary<string, object> dict => dict.ToDictionary(
				kvp => kvp.Key,
				kvp => DeepCloneValue(kvp.Value)),
			Dictionary<object, object> dict => dict.ToDictionary(
				kvp => kvp.Key?.ToString() ?? string.Empty,
				kvp => DeepCloneValue(kvp.Value)),
			IList<object> list => list.Select(DeepCloneValue).ToList(),
			System.Collections.IList list => list.Cast<object>().Select(DeepCloneValue).ToList(),
			_ => value // For primitive types and strings, which are immutable
		};
	}

	/// <summary>
	/// Converts a deserialized value to the appropriate type.
	/// </summary>
	/// <remarks>
	/// YamlDotNet resolves every alias to the same object as its anchor, so an alias inside its own anchor
	/// makes a cyclic graph and nested aliases make a graph far larger than its text. Copying either one
	/// recursively would overflow the stack, which cannot be caught, or exhaust memory, so both are
	/// rejected with an <see cref="InvalidOperationException"/> that reports the block as unreadable.
	/// </remarks>
	private static object ConvertValue(object? value, ConversionState state)
	{
		if (++state.ValueCount > MaxExpandedValues)
		{
			throw new InvalidOperationException($"The YAML expands to more than {MaxExpandedValues} values.");
		}

		if (value is not System.Collections.IEnumerable or string)
		{
			// A null stays null so it is written back as a null rather than as an empty string. The
			// dictionaries this feeds are typed as non-nullable for compatibility, so the null is forgiven.
			return value!;
		}

		if (!state.Ancestors.Add(value))
		{
			throw new InvalidOperationException("The YAML contains an alias that refers to itself.");
		}

		object converted = value switch
		{
			Dictionary<object, object> dict => dict.ToDictionary(
				kvp => kvp.Key?.ToString() ?? string.Empty,
				kvp => ConvertValue(kvp.Value, state)),
			List<object> list => list.Select(item => ConvertValue(item, state)).ToList(),
			System.Collections.IList list => list.Cast<object>().Select(item => ConvertValue(item, state)).ToList(),
			_ => value
		};

		state.Ancestors.Remove(value);
		return converted;
	}

	/// <summary>
	/// Tracks one block's conversion: the containers on the path being converted, to find a cycle, and
	/// the number of values converted so far, to bound alias expansion.
	/// </summary>
	private sealed class ConversionState
	{
		public HashSet<object> Ancestors { get; } = new(ReferenceComparer.Instance);

		public int ValueCount { get; set; }
	}

	/// <summary>
	/// Compares objects by reference. ReferenceEqualityComparer is not available on .NET Standard.
	/// </summary>
	private sealed class ReferenceComparer : IEqualityComparer<object>
	{
		public static ReferenceComparer Instance { get; } = new();

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
	}
}
