// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

/// <summary>
/// Normalizes frontmatter property names into a common form so that keys which differ only by
/// casing, separators, surrounding whitespace or a decorative prefix/suffix compare equal.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NameStandardizer"/> and <see cref="PropertyMerger"/> each carried their own copy of
/// this logic. The two had drifted apart: the standardizer lower-cased without trimming, stripped
/// its prefix and suffix with culture-sensitive comparisons, and replaced only the space character,
/// so a key carrying leading or trailing whitespace — a tab especially — kept it and missed the
/// prefix/suffix strip entirely. This single implementation takes the merger's behaviour, which is
/// the more robust of the two on every point of difference.
/// </para>
/// <para>
/// Trimming happens before the prefix and suffix strip so that <c>" page_title "</c> normalizes the
/// same as <c>"page_title"</c>, and <see cref="string.Trim()"/> covers every whitespace character
/// rather than the space alone. The prefix and suffix comparisons are ordinal because these are
/// fixed ASCII tokens, matched against an already invariantly-lowercased key.
/// </para>
/// </remarks>
internal static class PropertyNameNormalizer
{
	/// <summary>
	/// Decorative leading tokens that carry no meaning for matching purposes.
	/// </summary>
	private static readonly string[] Prefixes = ["page_", "post_", "meta_", "custom_", "user_", "site_"];

	/// <summary>
	/// Decorative trailing tokens that carry no meaning for matching purposes.
	/// </summary>
	private static readonly string[] Suffixes = ["_value", "_text", "_data", "_info", "_meta", "_field"];

	/// <summary>
	/// Characters treated as word separators within a property name.
	/// </summary>
	private static readonly char[] Separators = ['-', ' ', '_'];

	/// <summary>
	/// Normalizes a property name for comparison.
	/// </summary>
	/// <param name="key">The property name to normalize.</param>
	/// <returns>
	/// The name lower-cased and trimmed, with at most one decorative prefix and one decorative
	/// suffix removed, and all separator runs collapsed to a single underscore. Leading and
	/// trailing separators are dropped, so the result never begins or ends with an underscore.
	/// </returns>
	internal static string Normalize(string key)
	{
		key = key.Trim().ToLowerInvariant();

		foreach (string prefix in Prefixes)
		{
			if (key.StartsWith(prefix, StringComparison.Ordinal))
			{
				key = key[prefix.Length..];
				break;
			}
		}

		foreach (string suffix in Suffixes)
		{
			if (key.EndsWith(suffix, StringComparison.Ordinal))
			{
				key = key[..^suffix.Length];
				break;
			}
		}

		return string.Join("_", key.Split(Separators, StringSplitOptions.RemoveEmptyEntries));
	}

	/// <summary>
	/// Normalizes a property name and splits it into its constituent words.
	/// </summary>
	/// <param name="key">The property name to normalize and split.</param>
	/// <returns>The normalized name's words, with empty entries removed.</returns>
	internal static string[] NormalizeToWords(string key) =>
		Normalize(key).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
}
