// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>
/// Provides methods for processing and manipulating YAML frontmatter in markdown files.
/// </summary>
public static class Frontmatter
{
	/// <summary>
	/// The delimiter that marks the beginning and end of a frontmatter section.
	/// </summary>
	private const string FrontmatterDelimiter = "---";
	private const string DocumentEndMarker = "...";

	/// <summary>
	/// The byte order mark a document may begin with when it was decoded without stripping it.
	/// </summary>
	private const char ByteOrderMark = '\uFEFF';

	/// <summary>
	/// Cache for processed frontmatter to avoid repeated processing of identical content.
	/// Keyed by the document text together with the option flags it was processed under, so a cache
	/// hit means the input really is identical rather than merely hashing alike.
	/// </summary>
	private static readonly ConcurrentDictionary<(string Content, uint Options), string> ProcessedFrontmatterCache = new();

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input) =>
		CombineFrontmatter(input, FrontmatterNaming.Standard, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode) =>
		CombineFrontmatter(input, propertyNamingMode, FrontmatterOrder.Sorted, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <param name="orderMode">The ordering mode for frontmatter properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode, FrontmatterOrder orderMode) =>
		CombineFrontmatter(input, propertyNamingMode, orderMode, FrontmatterMergeStrategy.Conservative);

	/// <summary>
	/// Combines multiple frontmatter sections in a markdown document into a single frontmatter section.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="propertyNamingMode">The naming mode for frontmatter properties.</param>
	/// <param name="orderMode">The ordering mode for frontmatter properties.</param>
	/// <param name="mergeStrategy">The strategy for merging similar properties.</param>
	/// <returns>A string containing the markdown document with combined frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string CombineFrontmatter(string input, FrontmatterNaming propertyNamingMode, FrontmatterOrder orderMode, FrontmatterMergeStrategy mergeStrategy)
	{
		Ensure.NotNull(input);

		// Generate a unique cache key based on the content and options
		uint optionsHash = (uint)propertyNamingMode | ((uint)orderMode << 8) | ((uint)mergeStrategy << 16);
		(string Content, uint Options) cacheKey = (input, optionsHash);

		// Try to get from cache first
		if (ProcessedFrontmatterCache.TryGetValue(cacheKey, out string? cachedResult))
		{
			return cachedResult;
		}

		List<Dictionary<string, object>> frontmatterObjects = ExtractFrontmatterObjects(input, out string body, out bool hasUnreadableBlock);

		// A block that could not be parsed would be dropped from the rebuilt header while the body still
		// starts after it, so the document is left alone rather than losing that block's text.
		if (frontmatterObjects.Count == 0 || hasUnreadableBlock)
		{
			// Cache the original content since no processing was needed
			ProcessedFrontmatterCache.TryAdd(cacheKey, input);
			return input;
		}

		Dictionary<string, object> combinedFrontmatterObject = CombineAllFrontmatterObjects(frontmatterObjects);

		// Apply property merging if enabled
		if (mergeStrategy != FrontmatterMergeStrategy.None)
		{
			combinedFrontmatterObject = PropertyMerger.MergeSimilarProperties(combinedFrontmatterObject, mergeStrategy);
		}

		// Standardize property names using fuzzy matching if enabled
		if (propertyNamingMode == FrontmatterNaming.Standard)
		{
			combinedFrontmatterObject = NameStandardizer.StandardizePropertyNames(combinedFrontmatterObject);
		}

		// Sort properties according to standard conventions if enabled
		if (orderMode == FrontmatterOrder.Sorted)
		{
			combinedFrontmatterObject = SortFrontmatterProperties(combinedFrontmatterObject);
		}

		string combinedFrontmatter = YamlSerializer.SerializeYamlObject(combinedFrontmatterObject).Trim();

		string nl = Environment.NewLine;
		string result = $"{FrontmatterDelimiter}{nl}{combinedFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body}";

		// Cache the processed result
		ProcessedFrontmatterCache.TryAdd(cacheKey, result);

		return result;
	}

	/// <summary>
	/// Extracts frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A dictionary containing the frontmatter properties, or null if no frontmatter is found.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static Dictionary<string, object>? ExtractFrontmatter(string input)
	{
		Ensure.NotNull(input);

		if (!HasFrontmatter(input))
		{
			return null;
		}

		List<Dictionary<string, object>> frontmatterObjects = ExtractFrontmatterObjects(input, out _);
		return frontmatterObjects.Count > 0 ? frontmatterObjects.First() : null;
	}

	/// <summary>
	/// Checks if a markdown document contains frontmatter.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <remarks>
	/// Only a closed block counts. A document that opens with a <c>---</c> line that never closes, such as
	/// a markdown thematic break, has no frontmatter, so <see cref="AddFrontmatter"/> writes a header in front
	/// of it instead of leaving it alone.
	/// </remarks>
	/// <returns>True if the document contains frontmatter, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static bool HasFrontmatter(string input)
	{
		Ensure.NotNull(input);

		return TrySplitFrontmatterBlocks(input, out _, out _);
	}

	/// <summary>
	/// Checks whether the first line of a document is a frontmatter delimiter, whether or not a closing
	/// delimiter follows it.
	/// </summary>
	/// <param name="input">The document to inspect.</param>
	/// <returns>True if the document's first line is a delimiter, false otherwise.</returns>
	private static bool OpensWithDelimiter(string input)
	{
		// The opening delimiter follows the same rule as the closing one, so trailing whitespace an editor
		// left behind does not hide the header. A leading byte order mark is skipped, since callers that
		// decode bytes or streams themselves keep it where File.ReadAllText would strip it.
		int start = OpeningLineStart(input);
		int end = input.IndexOfAny(['\r', '\n'], start);
		return end >= 0 && IsDelimiterLine(input[start..end]);
	}

	/// <summary>
	/// Finds where the first line of a document starts, skipping a leading byte order mark.
	/// </summary>
	/// <param name="input">The document to inspect.</param>
	/// <returns>The offset of the first character after any byte order mark.</returns>
	private static int OpeningLineStart(string input) => input.Length > 0 && input[0] == ByteOrderMark ? 1 : 0;

	/// <summary>
	/// Adds frontmatter to a markdown document that doesn't already have it.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The frontmatter properties to add.</param>
	/// <returns>A string containing the markdown document with added frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string AddFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		Ensure.NotNull(input);

		if (frontmatter == null || frontmatter.Count == 0)
		{
			return input;
		}

		if (HasFrontmatter(input))
		{
			// Every existing block is folded in, since ReplaceFrontmatter keeps only the body after all of them.
			List<Dictionary<string, object>> existing = ExtractFrontmatterObjects(input, out _, out bool hasUnreadableBlock);

			// Frontmatter that is present but could not be read is left alone rather than overwritten, so a
			// gap in parsing loses nothing. Only a genuinely empty block is treated as having no properties.
			if (hasUnreadableBlock)
			{
				return input;
			}

			Dictionary<string, object> combined = CombineFrontmatterObjects(CombineAllFrontmatterObjects(existing), frontmatter);
			return ReplaceFrontmatter(input, combined);
		}

		string yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		string nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{TrimBody(input)}{nl}";
	}

	/// <summary>
	/// Replaces existing frontmatter in a markdown document with new frontmatter.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="frontmatter">The new frontmatter properties.</param>
	/// <returns>A string containing the markdown document with replaced frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string ReplaceFrontmatter(string input, Dictionary<string, object> frontmatter)
	{
		Ensure.NotNull(input);

		if (frontmatter == null || frontmatter.Count == 0)
		{
			return RemoveFrontmatter(input);
		}

		ExtractFrontmatterObjects(input, out string body);
		string yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		string nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{TrimBody(body)}{nl}";
	}

	/// <summary>
	/// Removes frontmatter from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing the markdown document with frontmatter removed.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string RemoveFrontmatter(string input)
	{
		Ensure.NotNull(input);

		if (!HasFrontmatter(input))
		{
			return input;
		}

		ExtractFrontmatterObjects(input, out string body);
		return TrimBody(body) + Environment.NewLine;
	}

	/// <summary>
	/// Extracts the document body (content after frontmatter) from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <returns>A string containing only the document body without frontmatter.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static string ExtractBody(string input)
	{
		Ensure.NotNull(input);

		ExtractFrontmatterObjects(input, out string body);
		return TrimBody(body);
	}

	/// <summary>
	/// Serializes a dictionary to YAML frontmatter format.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary to serialize.</param>
	/// <returns>A string containing the serialized YAML frontmatter.</returns>
	public static string SerializeFrontmatter(Dictionary<string, object> frontmatter) => frontmatter == null || frontmatter.Count == 0 ? string.Empty : YamlSerializer.SerializeYamlObject(frontmatter).Trim();

	/// <summary>
	/// Sorts frontmatter properties according to standard conventions.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary to sort.</param>
	/// <returns>A new dictionary with properties in the standard order.</returns>
	private static Dictionary<string, object> SortFrontmatterProperties(Dictionary<string, object> frontmatter)
	{
		// Create a new dictionary to hold the sorted properties
		Dictionary<string, object> sortedFrontmatter = [];

		// First add properties in the standard order (if they exist)
		foreach (string key in StandardOrder.PropertyNames)
		{
			if (frontmatter.TryGetValue(key, out object? value))
			{
				sortedFrontmatter[key] = value;
			}
		}

		// Then add any remaining properties that weren't in the standard order
		foreach (KeyValuePair<string, object> property in frontmatter)
		{
			if (!sortedFrontmatter.ContainsKey(property.Key))
			{
				sortedFrontmatter[property.Key] = property.Value;
			}
		}

		return sortedFrontmatter;
	}

	/// <summary>
	/// Extracts all frontmatter objects from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="body">Output parameter that will contain the markdown body without frontmatter.</param>
	/// <returns>A collection of dictionaries representing each frontmatter section.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there are too many frontmatter sections in the document.</exception>
	private static List<Dictionary<string, object>> ExtractFrontmatterObjects(string input, out string body) =>
		ExtractFrontmatterObjects(input, out body, out _);

	/// <summary>
	/// Extracts all frontmatter objects from a markdown document, reporting whether any of it could not be read.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="body">Output parameter that will contain the markdown body without frontmatter.</param>
	/// <param name="hasUnreadableBlock">
	/// True when a non-blank frontmatter block failed to parse. Such text is missing from the returned objects
	/// but not from the document. An opening delimiter that never closes is not frontmatter, so it is not
	/// reported here.
	/// </param>
	/// <returns>A collection of dictionaries representing each frontmatter section that parsed.</returns>
	private static List<Dictionary<string, object>> ExtractFrontmatterObjects(string input, out string body, out bool hasUnreadableBlock)
	{
		List<Dictionary<string, object>> frontmatterObjects = [];
		hasUnreadableBlock = false;

		if (!TrySplitFrontmatterBlocks(input, out List<string> blocks, out body))
		{
			return frontmatterObjects;
		}

		foreach (string block in blocks)
		{
			string section = block.Trim();
			if (string.IsNullOrWhiteSpace(section))
			{
				continue;
			}

			if (YamlSerializer.TryParseYamlObject(section, out Dictionary<string, object>? frontmatterObject) && frontmatterObject != null)
			{
				frontmatterObjects.Add(frontmatterObject);
			}
			else
			{
				hasUnreadableBlock = true;
			}
		}

		return frontmatterObjects;
	}

	/// <summary>
	/// Folds frontmatter objects into one, in document order, so an earlier block wins a repeated key.
	/// </summary>
	/// <param name="frontmatterObjects">The frontmatter objects to fold.</param>
	/// <returns>The combined frontmatter, empty when there are no objects.</returns>
	private static Dictionary<string, object> CombineAllFrontmatterObjects(List<Dictionary<string, object>> frontmatterObjects)
	{
		Dictionary<string, object> combined = [];
		foreach (Dictionary<string, object> frontmatterObject in frontmatterObjects)
		{
			combined = CombineFrontmatterObjects(combined, frontmatterObject);
		}

		return combined;
	}

	/// <summary>
	/// Splits the frontmatter blocks at the top of a document from its body.
	/// </summary>
	/// <remarks>
	/// Delimiters are recognised only as whole lines, so a <c>---</c> inside a value or a markdown
	/// horizontal rule in the body is never mistaken for one. The first line opens a block, which closes
	/// at the next delimiter line, including one at the very end of the document. YAML's document end
	/// marker <c>...</c>, which Pandoc metadata blocks use, also closes a block but never opens one; a
	/// rewritten document always closes its frontmatter with <c>---</c>. Further blocks are
	/// consumed only while each opens on the line straight after the previous one closed and holds
	/// frontmatter rather than body text (see <see cref="IsFollowOnBlock"/>); the body is everything after
	/// the last block consumed.
	/// </remarks>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="blocks">The raw text of each frontmatter block, in document order.</param>
	/// <param name="body">The document after the last frontmatter block, or the whole input when there is none.</param>
	/// <returns>True if at least one closed frontmatter block was found, false otherwise.</returns>
	private static bool TrySplitFrontmatterBlocks(string input, out List<string> blocks, out string body)
	{
		blocks = [];
		body = input;

		if (!OpensWithDelimiter(input))
		{
			return false;
		}

		List<(int Start, int End)> lines = SplitLines(input);
		lines[0] = (OpeningLineStart(input), lines[0].End);
		bool IsDelimiterAt(int index) => IsDelimiterLine(input[lines[index].Start..lines[index].End]);
		bool IsCloserAt(int index) => IsClosingDelimiterLine(input[lines[index].Start..lines[index].End]);

		int next = 0;
		while (next < lines.Count && IsDelimiterAt(next))
		{
			int close = next + 1;
			while (close < lines.Count && !IsCloserAt(close))
			{
				close++;
			}

			if (close == lines.Count)
			{
				break;
			}

			string block = close == next + 1
				? string.Empty
				: input[lines[next + 1].Start..lines[close - 1].End];

			// A thematic break placed directly under the header looks like the start of another block.
			// Only the first block is taken on trust; a later one must look like frontmatter, or it and
			// everything after it stay in the body.
			if (blocks.Count > 0 && !IsFollowOnBlock(block, lines, next, close, input))
			{
				break;
			}

			blocks.Add(block);
			next = close + 1;
		}

		if (blocks.Count == 0)
		{
			return false;
		}

		body = next < lines.Count ? input[lines[next].Start..] : string.Empty;
		return true;
	}

	/// <summary>
	/// Checks whether a block that follows the first one is really frontmatter rather than body text
	/// set between two thematic breaks.
	/// </summary>
	/// <remarks>
	/// A follow-on block has to hold a non-empty YAML mapping, and it may not open or close with a blank
	/// line. Stacked headers are written tight against their delimiters, whereas markdown around a
	/// thematic break is usually spaced out from it, so a blank line marks the block as body text even
	/// when that text happens to parse as YAML.
	/// </remarks>
	/// <param name="block">The raw text between the block's delimiters.</param>
	/// <param name="lines">The lines of the document.</param>
	/// <param name="open">The index of the block's opening delimiter line.</param>
	/// <param name="close">The index of the block's closing delimiter line.</param>
	/// <param name="input">The document.</param>
	/// <returns>True if the block should be read as frontmatter, false if it belongs to the body.</returns>
	private static bool IsFollowOnBlock(string block, List<(int Start, int End)> lines, int open, int close, string input)
	{
		bool IsBlankAt(int index) => string.IsNullOrWhiteSpace(input[lines[index].Start..lines[index].End]);

		return close > open + 1
			&& !IsBlankAt(open + 1)
			&& !IsBlankAt(close - 1)
			&& YamlSerializer.TryParseYamlObject(block.Trim(), out Dictionary<string, object>? properties)
			&& properties.Count > 0;
	}

	/// <summary>
	/// Finds the lines of a document, recognising CRLF, LF and CR endings wherever they occur.
	/// </summary>
	/// <param name="input">The document to split.</param>
	/// <returns>The start and end offset of each line, excluding its line ending.</returns>
	private static List<(int Start, int End)> SplitLines(string input)
	{
		List<(int Start, int End)> lines = [];
		int start = 0;

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c is not '\r' and not '\n')
			{
				continue;
			}

			lines.Add((start, i));
			if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
			{
				i++;
			}

			start = i + 1;
		}

		lines.Add((start, input.Length));
		return lines;
	}

	/// <summary>
	/// Trims a document body for output, dropping the blank lines before its first content line and the
	/// whitespace after its last. Unlike <see cref="string.Trim()"/>, it keeps the first content line's
	/// indentation, so a body that opens with an indented code block or nested list content survives.
	/// A leading byte order mark is dropped too: it is not whitespace, so it would otherwise be carried
	/// past a newly written header and hide the first line from markdown renderers.
	/// </summary>
	/// <param name="body">The body to trim.</param>
	/// <returns>The body without a byte order mark, leading blank lines or trailing whitespace.</returns>
	private static string TrimBody(string body)
	{
		int start = OpeningLineStart(body);
		for (int i = start; i < body.Length; i++)
		{
			char c = body[i];
			if (c == '\n')
			{
				start = i + 1;
			}
			else if (!char.IsWhiteSpace(c))
			{
				break;
			}
		}

		return body[start..].TrimEnd();
	}

	/// <summary>
	/// Checks whether a line is a frontmatter delimiter, allowing trailing whitespace.
	/// </summary>
	/// <param name="line">The line to check, without its line ending.</param>
	/// <returns>True if the line is a frontmatter delimiter, false otherwise.</returns>
	private static bool IsDelimiterLine(string line) => line.TrimEnd() == FrontmatterDelimiter;

	/// <summary>
	/// Checks whether a line can close a frontmatter block: a delimiter, or YAML's document end marker
	/// <c>...</c>, either allowing trailing whitespace.
	/// </summary>
	/// <param name="line">The line to check, without its line ending.</param>
	/// <returns>True if the line closes a frontmatter block, false otherwise.</returns>
	private static bool IsClosingDelimiterLine(string line) =>
		IsDelimiterLine(line) || line.TrimEnd() == DocumentEndMarker;

	/// <summary>
	/// Combines two frontmatter dictionaries into a single dictionary.
	/// </summary>
	/// <param name="a">The first frontmatter dictionary.</param>
	/// <param name="b">The second frontmatter dictionary.</param>
	/// <returns>A new dictionary containing the combined frontmatter.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there is a conflict between frontmatter values.</exception>
	private static Dictionary<string, object> CombineFrontmatterObjects(IDictionary<string, object> a, IDictionary<string, object> b)
	{
		Dictionary<string, object> combinedFrontmatterObject = [];

		// First, add all properties from dictionary a
		foreach (KeyValuePair<string, object> kvp in a)
		{
			combinedFrontmatterObject[kvp.Key] = kvp.Value;
		}

		// Then, add properties from dictionary b that don't exist in a
		foreach (KeyValuePair<string, object> kvp in b)
		{
			if (!combinedFrontmatterObject.ContainsKey(kvp.Key))
			{
				combinedFrontmatterObject[kvp.Key] = kvp.Value;
			}
		}

		return combinedFrontmatterObject;
	}
}
