// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ktsu.Extensions;

/// <summary>
/// Provides methods for processing and manipulating YAML frontmatter in markdown files.
/// </summary>
public static class Frontmatter
{
	/// <summary>
	/// The delimiter that marks the beginning and end of a frontmatter section.
	/// </summary>
	private const string FrontmatterDelimiter = "---";

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

		List<Dictionary<string, object>> frontmatterObjects = ExtractFrontmatterObjects(input, out string body);

		if (frontmatterObjects.Count == 0)
		{
			// Cache the original content since no processing was needed
			ProcessedFrontmatterCache.TryAdd(cacheKey, input);
			return input;
		}

		Dictionary<string, object> combinedFrontmatterObject = frontmatterObjects.First();
		foreach (Dictionary<string, object> frontmatterObject in frontmatterObjects.Skip(1))
		{
			combinedFrontmatterObject = CombineFrontmatterObjects(combinedFrontmatterObject, frontmatterObject);
		}

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
	/// <returns>True if the document contains frontmatter, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
	public static bool HasFrontmatter(string input)
	{
		Ensure.NotNull(input);

		return !string.IsNullOrEmpty(input) && input.StartsWithOrdinal(FrontmatterDelimiter + DetectNewLine(input));
	}

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
			// Document already has frontmatter, use CombineFrontmatter instead
			Dictionary<string, object>? existing = ExtractFrontmatter(input);

			// Frontmatter that is present but could not be read is left alone rather than overwritten, so a
			// gap in parsing loses nothing. Only a genuinely empty block is treated as having no properties.
			if (existing == null
				&& (!TrySplitFrontmatterBlocks(input, out List<string> blocks, out _) || blocks.Exists(block => !string.IsNullOrWhiteSpace(block))))
			{
				return input;
			}

			Dictionary<string, object> combined = CombineFrontmatterObjects(existing ?? [], frontmatter);
			return ReplaceFrontmatter(input, combined);
		}

		string yamlFrontmatter = YamlSerializer.SerializeYamlObject(frontmatter).Trim();
		string nl = Environment.NewLine;
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{input.Trim()}{nl}";
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
		return $"{FrontmatterDelimiter}{nl}{yamlFrontmatter}{nl}{FrontmatterDelimiter}{nl}{body.Trim()}{nl}";
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
		return body.Trim() + Environment.NewLine;
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
		return body.Trim();
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
	/// Finds the line ending a document actually uses, rather than assuming the host's.
	/// </summary>
	/// <remarks>
	/// A markdown file written on Windows still holds CRLF when it is read on Linux, and one
	/// written on Linux still holds LF when it is read on Windows. Line endings travel with the
	/// document, so parsing has to follow the document rather than the machine it is parsed on.
	/// Writing is a separate question and still uses the host convention.
	/// </remarks>
	/// <param name="input">The document to inspect.</param>
	/// <returns>The ending that terminates the first line, or the host's when there is none.</returns>
	private static string DetectNewLine(string input)
	{
		// The first line is the opening delimiter, so its terminator is the one the delimiter
		// search has to match. Scanning the whole document instead would pick up a stray ending
		// from the body, and a document written here with LF but quoting CRLF content would then
		// report CRLF and fail to match the delimiter it had just written itself.
		int index = input.IndexOfAny(['\r', '\n']);

		return index < 0
			? Environment.NewLine
			: input[index] == '\n' ? "\n"
			: index + 1 < input.Length && input[index + 1] == '\n' ? "\r\n"
			: "\r";
	}

	/// <summary>
	/// Extracts all frontmatter objects from a markdown document.
	/// </summary>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="body">Output parameter that will contain the markdown body without frontmatter.</param>
	/// <returns>A collection of dictionaries representing each frontmatter section.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there are too many frontmatter sections in the document.</exception>
	private static List<Dictionary<string, object>> ExtractFrontmatterObjects(string input, out string body)
	{
		List<Dictionary<string, object>> frontmatterObjects = [];

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
		}

		return frontmatterObjects;
	}

	/// <summary>
	/// Splits the frontmatter blocks at the top of a document from its body.
	/// </summary>
	/// <remarks>
	/// Delimiters are recognised only as whole lines, so a <c>---</c> inside a value or a markdown
	/// horizontal rule in the body is never mistaken for one. The first line opens a block, which closes
	/// at the next delimiter line, including one at the very end of the document. Further blocks are
	/// consumed only while each opens on the line straight after the previous one closed; the body is
	/// everything after the last block consumed.
	/// </remarks>
	/// <param name="input">The markdown document content as a string.</param>
	/// <param name="blocks">The raw text of each frontmatter block, in document order.</param>
	/// <param name="body">The document after the last frontmatter block, or the whole input when there is none.</param>
	/// <returns>True if at least one closed frontmatter block was found, false otherwise.</returns>
	private static bool TrySplitFrontmatterBlocks(string input, out List<string> blocks, out string body)
	{
		blocks = [];
		body = input;

		if (!HasFrontmatter(input))
		{
			return false;
		}

		List<(int Start, int End)> lines = SplitLines(input);
		bool IsDelimiterAt(int index) => IsDelimiterLine(input[lines[index].Start..lines[index].End]);

		int next = 0;
		while (next < lines.Count && IsDelimiterAt(next))
		{
			int close = next + 1;
			while (close < lines.Count && !IsDelimiterAt(close))
			{
				close++;
			}

			if (close == lines.Count)
			{
				break;
			}

			blocks.Add(close == next + 1
				? string.Empty
				: input[lines[next + 1].Start..lines[close - 1].End]);
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
	/// Checks whether a line is a frontmatter delimiter, allowing trailing whitespace.
	/// </summary>
	/// <param name="line">The line to check, without its line ending.</param>
	/// <returns>True if the line is a frontmatter delimiter, false otherwise.</returns>
	private static bool IsDelimiterLine(string line) => line.TrimEnd() == FrontmatterDelimiter;

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
