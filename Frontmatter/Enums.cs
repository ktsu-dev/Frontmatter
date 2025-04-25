namespace ktsu.Frontmatter;

/// <summary>
/// Defines naming modes for frontmatter properties.
/// </summary>
public enum FrontmatterNaming
{
	/// <summary>
	/// Keep property names as-is without standardization.
	/// </summary>
	AsIs,

	/// <summary>
	/// Standardize property names using common conventions.
	/// </summary>
	Standard
}

/// <summary>
/// Defines ordering modes for frontmatter properties.
/// </summary>
public enum FrontmatterOrder
{
	/// <summary>
	/// Keep properties in the order they appear in the document.
	/// </summary>
	AsIs,

	/// <summary>
	/// Sort properties according to standard conventions.
	/// </summary>
	Sorted
}

/// <summary>
/// Defines strategies for merging similar frontmatter properties.
/// </summary>
public enum FrontmatterMergeStrategy
{
	/// <summary>
	/// Do not merge any properties.
	/// </summary>
	None,

	/// <summary>
	/// Only merge properties using predefined mappings.
	/// </summary>
	Conservative,

	/// <summary>
	/// Merge properties using basic pattern matching and predefined mappings.
	/// </summary>
	Aggressive,

	/// <summary>
	/// Merge properties using semantic analysis, fuzzy matching, and all available strategies.
	/// </summary>
	Maximum
}
