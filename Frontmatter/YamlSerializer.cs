// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

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
	/// The YAML 1.1 merge key, which merges the keys of another mapping, or of a sequence of mappings, into
	/// the mapping it is written in.
	/// </summary>
	private const string MergeKey = "<<";

	/// <summary>
	/// Stands in for a quoted <c>"&lt;&lt;"</c> scalar while a block is deserialized. Only a plain <c>&lt;&lt;</c>
	/// is the merge key; a quoted one is an ordinary string, but the deserialized dictionary no longer records
	/// how a key was written. The two noncharacters keep the stand-in from matching any key a document holds.
	/// </summary>
	private const string QuotedMergeKey = "\uFDD0<<\uFDD1";

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
	private static readonly ISerializer Serializer = CreateSerializerBuilder()
		.WithQuotingNecessaryStrings()
		.Build();

	/// <summary>
	/// Serializer for values a caller supplies. It also quotes strings that YAML 1.1 reads as booleans or
	/// numbers, such as <c>no</c>, <c>on</c> or <c>12:30</c>, so Jekyll, PyYAML and other YAML 1.1 readers
	/// see them as strings too. Values read from a document are not written with it: a plain <c>yes</c> is
	/// read as the string "yes", and quoting it would change what a YAML 1.1 reader sees in the other direction.
	/// </summary>
	private static readonly ISerializer CallerValueSerializer = CreateSerializerBuilder()
		.WithQuotingNecessaryStrings(quoteYaml1_1Strings: true)
		.Build();

	/// <summary>
	/// Creates the builder both serializers start from, so an option that one of them needs, such as the
	/// invariant scalar converter, cannot be left off the other. Only string quoting differs between them.
	/// </summary>
	/// <remarks>
	/// Aliases are disabled, so an object the caller passes under two keys is written in full under each
	/// instead of as an anchor (<c>&amp;o0</c>) and an alias (<c>*o0</c>). A header is text people edit, and
	/// readers such as Ruby's <c>YAML.safe_load</c> refuse aliases.
	/// </remarks>
	/// <returns>A serializer builder with every option the two serializers share.</returns>
	private static SerializerBuilder CreateSerializerBuilder() => new SerializerBuilder()
		.DisableAliases()
		.WithNamingConvention(NullNamingConvention.Instance)
		.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
		.WithEventEmitter(next => new EdgeWhitespaceQuotingEventEmitter(next))
		.WithTypeConverter(new InvariantScalarTypeConverter());

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
			Dictionary<object, object>? rawData = Deserializer.Deserialize<Dictionary<object, object>?>(new QuotedMergeKeyParser(new TagTolerantParser(new Parser(new StringReader(input)))));
			if (rawData is null)
			{
				return false;
			}

			result = [];
			ConversionState state = new();
			state.Ancestors.Add(rawData);

			// Convert dictionary keys to strings and preserve the first occurrence of duplicate keys
			foreach (KeyValuePair<object, object> pair in ResolveMergeKeys(rawData, state))
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
			object? value = Deserializer.Deserialize<object?>(new TagTolerantParser(new Parser(new StringReader(input))));
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
	/// <exception cref="ArgumentException">Thrown when a value contains itself.</exception>
	public static string SerializeYamlObject(Dictionary<string, object> input) =>
		input == null || input.Count == 0 ? string.Empty : Serialize(Serializer, input);

	/// <summary>
	/// Serializes properties a caller supplied to a YAML string, quoting strings that a YAML 1.1 reader
	/// would take for a boolean or a number as well as those a YAML 1.2 reader would.
	/// </summary>
	/// <param name="input">The dictionary to serialize.</param>
	/// <returns>A string containing the serialized YAML.</returns>
	/// <exception cref="ArgumentException">Thrown when a value contains itself.</exception>
	internal static string SerializeCallerValues(Dictionary<string, object> input) =>
		input == null || input.Count == 0 ? string.Empty : Serialize(CallerValueSerializer, input);

	/// <summary>
	/// Serializes properties, reporting a value that contains itself as an <see cref="ArgumentException"/>
	/// naming its key. With aliases disabled such a value cannot be written, and YamlDotNet stops at its
	/// recursion limit instead.
	/// </summary>
	/// <param name="serializer">The serializer to write with.</param>
	/// <param name="input">The properties to serialize.</param>
	/// <returns>A string containing the serialized YAML.</returns>
	/// <exception cref="ArgumentException">Thrown when a value contains itself.</exception>
	private static string Serialize(ISerializer serializer, Dictionary<string, object> input)
	{
		try
		{
			return serializer.Serialize(input);
		}
		catch (MaximumRecursionLevelReachedException ex)
		{
			string key = input.Keys.FirstOrDefault(k => !CanSerialize(serializer, k, input[k])) ?? string.Empty;
			throw new ArgumentException($"The value of frontmatter property '{key}' contains itself, so it cannot be written as YAML.", nameof(input), ex);
		}
	}

	/// <summary>
	/// Reports whether one property can be serialized without reaching the recursion limit.
	/// </summary>
	/// <param name="serializer">The serializer to write with.</param>
	/// <param name="key">The property name.</param>
	/// <param name="value">The property value.</param>
	/// <returns>true if the property serializes; otherwise, false.</returns>
	private static bool CanSerialize(ISerializer serializer, string key, object value)
	{
		try
		{
			serializer.Serialize(new Dictionary<string, object> { [key] = value });
			return true;
		}
		catch (MaximumRecursionLevelReachedException)
		{
			return false;
		}
	}

	/// <summary>
	/// Serializes properties for a document that already has a header, writing each value the header already
	/// holds unchanged the way a value read from a document is written, and every other value the way a
	/// caller-supplied value is written.
	/// </summary>
	/// <remarks>
	/// A value read from a document is not given YAML 1.1 quoting, so rewriting a header the caller only
	/// partly changed keeps <c>published: no</c> or <c>last_seen: 12:30</c> as written instead of turning
	/// them into strings. A value is treated as unchanged when it serializes exactly as the header's value
	/// for the same key does.
	/// </remarks>
	/// <param name="input">The dictionary to serialize.</param>
	/// <param name="documentValues">The properties the document's existing header holds.</param>
	/// <returns>A string containing the serialized YAML.</returns>
	internal static string SerializeValuesForDocument(Dictionary<string, object> input, IReadOnlyDictionary<string, object> documentValues)
	{
		if (input == null || input.Count == 0)
		{
			return string.Empty;
		}

		if (documentValues == null || documentValues.Count == 0)
		{
			return Serialize(CallerValueSerializer, input);
		}

		// Consecutive keys written by the same serializer are serialized together, keeping the caller's key order.
		System.Text.StringBuilder yaml = new();
		Dictionary<string, object> run = [];
		bool runIsFromDocument = false;
		foreach (KeyValuePair<string, object> property in input)
		{
			bool fromDocument = IsUnchangedDocumentValue(property.Key, property.Value, documentValues);
			if (run.Count > 0 && fromDocument != runIsFromDocument)
			{
				yaml.Append(Serialize(runIsFromDocument ? Serializer : CallerValueSerializer, run));
				run = [];
			}

			runIsFromDocument = fromDocument;
			run[property.Key] = property.Value;
		}

		yaml.Append(Serialize(runIsFromDocument ? Serializer : CallerValueSerializer, run));
		return yaml.ToString();
	}

	/// <summary>
	/// Reports whether a value is the one the document's header already holds for its key.
	/// </summary>
	/// <param name="key">The property name.</param>
	/// <param name="value">The value about to be written.</param>
	/// <param name="documentValues">The properties the document's existing header holds.</param>
	/// <returns>true if the header holds the key and its value serializes the same way; otherwise, false.</returns>
	private static bool IsUnchangedDocumentValue(string key, object value, IReadOnlyDictionary<string, object> documentValues)
	{
		if (!documentValues.TryGetValue(key, out object? documentValue))
		{
			return false;
		}

		if (ReferenceEquals(value, documentValue))
		{
			return true;
		}

		return Serialize(Serializer, new Dictionary<string, object> { [key] = value })
			== Serialize(Serializer, new Dictionary<string, object> { [key] = documentValue });
	}

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

		if (value is QuotedMergeKey)
		{
			return MergeKey;
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
			Dictionary<object, object> dict => ResolveMergeKeys(dict, state).ToDictionary(
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
	/// Returns a mapping's entries with its YAML 1.1 merge key (<c>&lt;&lt;: *anchor</c>) resolved into the
	/// keys it merges, as Jekyll, PyYAML and js-yaml read it, rather than kept as a literal <c>&lt;&lt;</c> key.
	/// </summary>
	/// <remarks>
	/// The merged keys take the merge key's place. A key written in the mapping itself wins over a merged
	/// one wherever it appears, and in a sequence of mappings an earlier mapping wins over a later one, as
	/// the merge key type specifies. A merge key whose value is neither a mapping nor a sequence of mappings
	/// is kept as an ordinary key. A mapping merged into itself is rejected as a cycle, and merged keys count
	/// towards <see cref="MaxExpandedValues"/>, so both report the block as unreadable.
	/// </remarks>
	private static List<KeyValuePair<object, object>> ResolveMergeKeys(Dictionary<object, object> mapping, ConversionState state)
	{
		if (!mapping.TryGetValue(MergeKey, out object? merge) || !TryGetMergeSources(merge, out List<Dictionary<object, object>>? sources))
		{
			return [.. mapping];
		}

		HashSet<string> written = [.. mapping.Keys.Where(key => key is not MergeKey).Select(ScalarKey)];
		List<KeyValuePair<object, object>> resolved = [];
		foreach (KeyValuePair<object, object> pair in mapping)
		{
			if (pair.Key is not MergeKey)
			{
				resolved.Add(pair);
				continue;
			}

			foreach (Dictionary<object, object> source in sources)
			{
				AddMergedKeys(source, written, resolved, state);
			}
		}

		return resolved;
	}

	/// <summary>
	/// Adds the keys of one merged mapping, its own merge keys resolved, that are not already written.
	/// </summary>
	private static void AddMergedKeys(Dictionary<object, object> source, HashSet<string> written, List<KeyValuePair<object, object>> resolved, ConversionState state)
	{
		if (!state.Ancestors.Add(source))
		{
			throw new InvalidOperationException("The YAML merges a mapping into itself.");
		}

		foreach (KeyValuePair<object, object> merged in ResolveMergeKeys(source, state))
		{
			if (++state.ValueCount > MaxExpandedValues)
			{
				throw new InvalidOperationException($"The YAML expands to more than {MaxExpandedValues} values.");
			}

			if (written.Add(ScalarKey(merged.Key)))
			{
				resolved.Add(merged);
			}
		}

		state.Ancestors.Remove(source);
	}

	/// <summary>
	/// Reads the value of a merge key as the mappings it merges: a single mapping, or a sequence of them.
	/// </summary>
	private static bool TryGetMergeSources(object? merge, [NotNullWhen(true)] out List<Dictionary<object, object>>? sources)
	{
		sources = merge switch
		{
			Dictionary<object, object> mapping => [mapping],
			List<object> list when list.All(item => item is Dictionary<object, object>) => [.. list.Cast<Dictionary<object, object>>()],
			_ => null,
		};

		return sources is not null;
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
	private static string ScalarKey(object key) => key switch
	{
		QuotedMergeKey => MergeKey,
		System.Collections.IEnumerable and not string => throw new InvalidOperationException("The YAML contains a key that is not a scalar."),
		_ => key.ToString()!,
	};

	/// <summary>
	/// Reports whether a YAML document holds a tag that <see cref="TryParseYamlObject"/> drops while reading it,
	/// such as R Markdown's <c>!r</c> or <c>!expr</c>, or <c>!!binary</c>, <c>!!set</c> or <c>!!omap</c>.
	/// </summary>
	/// <remarks>
	/// Such a document can be read, but the values read from it no longer say what the tag said, so writing
	/// them back would silently lose the tag. The core tags that only restate a node's kind or type
	/// (<c>!!str</c>, <c>!!null</c>, <c>!!bool</c>, <c>!!int</c>, <c>!!float</c>, <c>!!seq</c>, <c>!!map</c> and the
	/// non-specific <c>!</c>) are read faithfully, so they are not reported.
	/// </remarks>
	/// <param name="input">The YAML text.</param>
	/// <returns>true if reading the document drops a tag; otherwise, false.</returns>
	internal static bool HoldsDroppedTag(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}

		try
		{
			Parser parser = new(new StringReader(input));
			while (parser.MoveNext())
			{
				if (parser.Current is NodeEvent { Tag.IsEmpty: false } node && TagTolerantParser.KindOf(node.Tag.Value) == CoreTag.None)
				{
					return true;
				}
			}
		}
		catch (YamlException)
		{
			return false;
		}

		return false;
	}

	/// <summary>
	/// The core tags <see cref="TagTolerantParser"/> reads faithfully.
	/// </summary>
	private enum CoreTag
	{
		None,
		String,
		Null,
		Scalar,
		Collection,
	}

	/// <summary>
	/// Removes the tags from a document's nodes, so YamlDotNet, which has no mapping for them, does not reject
	/// the whole document with "Encountered an unresolved tag" (#243).
	/// </summary>
	/// <remarks>
	/// <c>!!str</c> and the non-specific <c>!</c> make a scalar a string, as a quoted scalar is.
	/// <c>!!null</c> makes it null. <c>!!bool</c>, <c>!!int</c> and <c>!!float</c> have the value resolved as a plain
	/// scalar would be, and <c>!!seq</c> and <c>!!map</c> only restate the collection's kind. Any other tag is
	/// dropped, so its node is read as the untagged scalar or collection it is written as.
	/// <see cref="HoldsDroppedTag"/> reports a document that holds such a tag.
	/// </remarks>
	private sealed class TagTolerantParser(IParser parser) : IParser
	{
		private const string CoreTagPrefix = "tag:yaml.org,2002:";

		public ParsingEvent? Current { get; private set; }

		public bool MoveNext()
		{
			if (!parser.MoveNext())
			{
				Current = null;
				return false;
			}

			Current = parser.Current switch
			{
				Scalar { Tag.IsEmpty: false } scalar => Untagged(scalar),
				MappingStart { Tag.IsEmpty: false } mapping => new MappingStart(mapping.Anchor, TagName.Empty, mapping.IsImplicit, mapping.Style, mapping.Start, mapping.End),
				SequenceStart { Tag.IsEmpty: false } sequence => new SequenceStart(sequence.Anchor, TagName.Empty, sequence.IsImplicit, sequence.Style, sequence.Start, sequence.End),
				_ => parser.Current,
			};
			return true;
		}

		/// <summary>
		/// Classifies a tag as one of the core tags read faithfully, or as one that is dropped.
		/// </summary>
		/// <param name="tag">The tag's full name, such as <c>tag:yaml.org,2002:str</c>.</param>
		/// <returns>The kind of core tag, or <see cref="CoreTag.None"/> for a tag reading drops.</returns>
		internal static CoreTag KindOf(string tag) => tag switch
		{
			"!" or CoreTagPrefix + "str" => CoreTag.String,
			CoreTagPrefix + "null" => CoreTag.Null,
			CoreTagPrefix + "bool" or CoreTagPrefix + "int" or CoreTagPrefix + "float" => CoreTag.Scalar,
			CoreTagPrefix + "seq" or CoreTagPrefix + "map" => CoreTag.Collection,
			_ => CoreTag.None,
		};

		private static Scalar Untagged(Scalar scalar) => KindOf(scalar.Tag.Value) switch
		{
			CoreTag.String => new Scalar(scalar.Anchor, TagName.Empty, scalar.Value, scalar.Style is ScalarStyle.Plain or ScalarStyle.Any ? ScalarStyle.DoubleQuoted : scalar.Style, false, true, scalar.Start, scalar.End),
			CoreTag.Null => new Scalar(scalar.Anchor, TagName.Empty, string.Empty, ScalarStyle.Plain, true, false, scalar.Start, scalar.End),
			CoreTag.Scalar => new Scalar(scalar.Anchor, TagName.Empty, scalar.Value, ScalarStyle.Plain, true, false, scalar.Start, scalar.End),
			_ => new Scalar(scalar.Anchor, TagName.Empty, scalar.Value, scalar.Style, scalar.IsPlainImplicit, scalar.IsQuotedImplicit, scalar.Start, scalar.End),
		};
	}

	/// <summary>
	/// Replaces a single- or double-quoted <c>&lt;&lt;</c> scalar with <see cref="QuotedMergeKey"/>, so that
	/// <see cref="ResolveMergeKeys"/> resolves only a plain <c>&lt;&lt;</c> as a merge, as YAML 1.1, PyYAML and
	/// js-yaml do. The stand-in is turned back into <c>&lt;&lt;</c> when the block is converted.
	/// </summary>
	private sealed class QuotedMergeKeyParser(IParser parser) : IParser
	{
		public ParsingEvent? Current { get; private set; }

		public bool MoveNext()
		{
			if (!parser.MoveNext())
			{
				Current = null;
				return false;
			}

			Current = parser.Current is Scalar { Value: MergeKey, Style: not (ScalarStyle.Plain or ScalarStyle.Any) } scalar
				? new Scalar(scalar.Anchor, scalar.Tag, QuotedMergeKey, scalar.Style, scalar.IsPlainImplicit, scalar.IsQuotedImplicit, scalar.Start, scalar.End)
				: parser.Current;
			return true;
		}
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
	/// Writes <see cref="DateTimeOffset"/>, <see cref="System.Version"/>, and on .NET 6 or later <c>DateOnly</c> and
	/// <c>TimeOnly</c>, as one invariant scalar. Without it YamlDotNet writes each as a mapping of its public
	/// properties, which site generators cannot read as a date or version.
	/// </summary>
	/// <remarks>
	/// Dates are written plain in ISO 8601, so YAML readers such as Jekyll resolve them as timestamps. A time
	/// is double-quoted, since YAML 1.1 reads a plain <c>10:30:00</c> as a base-60 number. A version is written
	/// as a string, which quotes one such as <c>1.2</c> that would otherwise read back as a number. A
	/// <c>TimeSpan</c> is double-quoted in its invariant <c>"c"</c> format for the same reason as a time (#225).
	/// A <c>double</c> or <c>float</c> in exponent form gets a <c>.</c> in its mantissa, as in <c>1.0E+20</c>,
	/// since YAML 1.1 reads <c>1E+20</c> as a string while YAML 1.2 reads either as a float (#225). Every
	/// format is culture-invariant, unlike YamlDotNet's own <c>DateOnly</c> and <c>TimeOnly</c> converters.
	/// </remarks>
	private sealed class InvariantScalarTypeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) =>
			type == typeof(DateTimeOffset)
			|| type == typeof(System.Version)
			|| type == typeof(TimeSpan)
			|| type == typeof(double)
			|| type == typeof(float)
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
				case TimeSpan duration:
					emitter.Emit(new Scalar(null, null, duration.ToString("c", culture), ScalarStyle.DoubleQuoted, true, true));
					break;
				case double number:
					emitter.Emit(new Scalar(FormatFloat(number, number.ToString("R", culture))));
					break;
				case float number:
					emitter.Emit(new Scalar(FormatFloat(number, number.ToString("R", culture))));
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

		/// <summary>
		/// Writes a floating-point number so that YAML 1.1 and YAML 1.2 readers both read a float. Infinity and
		/// NaN take YAML's own spellings. An exponent form such as <c>1E+20</c> gets <c>.0</c> after its mantissa,
		/// since YAML 1.1's float pattern needs a <c>.</c>; the exponent is already signed.
		/// </summary>
		/// <param name="number">The number, widened to a double.</param>
		/// <param name="text">The number's invariant round-trip text.</param>
		/// <returns>The text to write as a plain scalar.</returns>
		private static string FormatFloat(double number, string text)
		{
			if (double.IsNaN(number))
			{
				return ".nan";
			}

			if (double.IsInfinity(number))
			{
				return number > 0 ? ".inf" : "-.inf";
			}

			int exponent = text.IndexOfAny(['E', 'e']);
			return exponent >= 0 && text.IndexOf('.', 0, exponent) < 0
				? text[..exponent] + ".0" + text[exponent..]
				: text;
		}
	}

	/// <summary>
	/// Double-quotes any string that starts or ends with whitespace, or that holds a carriage return or
	/// NEL. YAML strips edge whitespace from a plain scalar, and <c>WithQuotingNecessaryStrings</c> does not
	/// quote a string that ends in a tab. A string with <c>\r</c> or U+0085 would otherwise be written as a
	/// block scalar, whose line breaks all read back as <c>\n</c>. Without this the value would come back
	/// changed on the next read.
	/// </summary>
	/// <remarks>
	/// It also double-quotes strings that a reader would not take for strings although
	/// <c>quoteYaml1_1Strings</c> leaves them plain. That flag replaces YAML 1.2's float pattern with
	/// YAML 1.1's, which needs a <c>.</c> and a signed exponent, so <c>1e3</c> or <c>2e10</c> would be read back
	/// as numbers by this library and by js-yaml (#228). It also leaves YAML 1.1's value indicator <c>=</c> and
	/// merge indicator <c>&lt;&lt;</c> plain, which PyYAML's <c>safe_load</c> cannot construct, so it rejects the
	/// whole header (#230). The document serializer already quotes the float spellings, and a plain
	/// <c>=</c> or <c>&lt;&lt;</c> it writes quoted only stops a YAML 1.1 reader from failing on it.
	/// </remarks>
	private sealed class EdgeWhitespaceQuotingEventEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
	{
		private static readonly char[] NormalizedLineBreaks = ['\r', '\u0085'];

		/// <summary>
		/// The YAML 1.2 core schema's float pattern, without the <c>.inf</c> and <c>.nan</c> spellings, which
		/// the YAML 1.1 quoting already covers.
		/// </summary>
		private static readonly Regex Yaml12Float = new(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

		public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
		{
			if (eventInfo.Source.Value is string { Length: > 0 } text
				&& (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]) || text.IndexOfAny(NormalizedLineBreaks) >= 0
					|| IsNonStringLookalike(text)))
			{
				eventInfo.Style = ScalarStyle.DoubleQuoted;
			}

			base.Emit(eventInfo, emitter);
		}

		private static bool IsNonStringLookalike(string text) =>
			text is "=" or MergeKey || Yaml12Float.IsMatch(text);
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
