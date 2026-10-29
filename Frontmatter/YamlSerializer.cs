// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Provides utilities for parsing and serializing YAML content.
/// </summary>
public static class YamlSerializer
{
	/// <summary>
	/// Cache for parsed YAML to avoid repeated parsing.
	/// Keyed by the YAML text itself, compared ordinally, so a cache hit means the input really is
	/// identical rather than merely hashing alike. It is bounded so a long-running host does not keep every
	/// block it has parsed.
	/// </summary>
	internal static readonly BoundedCache<string, Dictionary<string, object>> ParsedYamlCache = new(capacity: 1024, StringComparer.Ordinal);

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
		.WithEventEmitter(next => new EdgeWhitespaceQuotingEventEmitter(next))
		.WithTypeConverter(new InvariantScalarTypeConverter())
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
					string key = ScalarKey(pair.Key);
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

				ParsedYamlCache.Add(input, cacheResult);
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
	/// Reports whether a YAML document parses but holds no keys: a comment only, <c>~</c>, <c>null</c> or <c>{}</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="TryParseYamlObject"/> returns false for such a document as well as for one that fails to parse;
	/// this tells the two apart, so an empty header can be treated as empty rather than unreadable.
	/// </remarks>
	/// <param name="input">The YAML text.</param>
	/// <returns>true if the document parses to nothing or to an empty mapping; otherwise, false.</returns>
	internal static bool IsEmptyYamlDocument(string input)
	{
		try
		{
			object? value = Deserializer.Deserialize<object?>(input);
			return value is null or System.Collections.IDictionary { Count: 0 };
		}
		catch (Exception ex) when (ex is YamlException or InvalidOperationException or ArgumentException)
		{
			return false;
		}
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
				kvp => kvp.Key is null ? string.Empty : ScalarKey(kvp.Key),
				kvp => ConvertValue(kvp.Value, state)),
			List<object> list => list.Select(item => ConvertValue(item, state)).ToList(),
			System.Collections.IList list => list.Cast<object>().Select(item => ConvertValue(item, state)).ToList(),
			_ => value
		};

		state.Ancestors.Remove(value);
		return converted;
	}

	/// <summary>
	/// Returns the text of a mapping key.
	/// </summary>
	/// <remarks>
	/// A complex key (<c>? [a, b]</c> or <c>? {x: 1}</c>) deserializes to a list or dictionary, whose
	/// <see cref="object.ToString"/> is its .NET type name. Writing that back would replace the key, so the
	/// block is reported as unreadable instead with an <see cref="InvalidOperationException"/>, and the
	/// callers leave it alone.
	/// </remarks>
	private static string ScalarKey(object key) =>
		key is System.Collections.IEnumerable and not string
			? throw new InvalidOperationException("The YAML contains a key that is not a scalar.")
			: key.ToString()!;

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
	/// Writes <see cref="DateTimeOffset"/>, <see cref="System.Version"/>, and on .NET 6 or later <c>DateOnly</c> and
	/// <c>TimeOnly</c>, as one invariant scalar. Without it YamlDotNet writes each as a mapping of its public
	/// properties, which site generators cannot read as a date or version.
	/// </summary>
	/// <remarks>
	/// Dates are written plain in ISO 8601, so YAML readers such as Jekyll resolve them as timestamps. A time
	/// is double-quoted, since YAML 1.1 reads a plain <c>10:30:00</c> as a base-60 number. A version is written
	/// as a string, which quotes one such as <c>1.2</c> that would otherwise read back as a number. Every
	/// format is culture-invariant, unlike YamlDotNet's own <c>DateOnly</c> and <c>TimeOnly</c> converters.
	/// </remarks>
	private sealed class InvariantScalarTypeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) =>
			type == typeof(DateTimeOffset)
			|| type == typeof(System.Version)
#if NET6_0_OR_GREATER
			|| type == typeof(DateOnly)
			|| type == typeof(TimeOnly)
#endif
			;

		// The converter is registered on the serializer only; reading leaves these values as the scalars written.
		public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) =>
			throw new NotSupportedException("This converter only writes values.");

		public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
		{
			CultureInfo culture = CultureInfo.InvariantCulture;
			switch (value)
			{
				case DateTimeOffset dateTimeOffset:
					emitter.Emit(new Scalar(dateTimeOffset.ToString("O", culture)));
					break;
#if NET6_0_OR_GREATER
				case DateOnly date:
					emitter.Emit(new Scalar(date.ToString("yyyy-MM-dd", culture)));
					break;
				case TimeOnly time:
					emitter.Emit(new Scalar(null, null, time.ToString("HH:mm:ss.FFFFFFF", culture), ScalarStyle.DoubleQuoted, true, true));
					break;
#endif
				default:
					// A Version, which Accepts leaves as the only other type.
					serializer(value?.ToString(), typeof(string));
					break;
			}
		}
	}

	/// <summary>
	/// Double-quotes any string that starts or ends with whitespace, or that holds a carriage return or
	/// NEL. YAML strips edge whitespace from a plain scalar, and <c>WithQuotingNecessaryStrings</c> does not
	/// quote a string that ends in a tab. A string with <c>\r</c> or U+0085 would otherwise be written as a
	/// block scalar, whose line breaks all read back as <c>\n</c>. Without this the value would come back
	/// changed on the next read.
	/// </summary>
	private sealed class EdgeWhitespaceQuotingEventEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
	{
		private static readonly char[] NormalizedLineBreaks = ['\r', '\u0085'];

		public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
		{
			if (eventInfo.Source.Value is string { Length: > 0 } text
				&& (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]) || text.IndexOfAny(NormalizedLineBreaks) >= 0))
			{
				eventInfo.Style = ScalarStyle.DoubleQuoted;
			}

			base.Emit(eventInfo, emitter);
		}
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
