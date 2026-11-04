// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #234: adding a number that does not read back exactly, such as a double that is
/// not exact in float32 or a decimal with a trailing zero, still inserts it into the header as written
/// rather than rebuilding the header and dropping its comments.
/// </summary>
[TestClass]
public class AddNonFloat32ExactNumberTests
{
	private const string Input = "---\n# Venue page; do not publish until confirmed\ntitle: Venue\npublished: no\n---\nBody\n";

	[TestMethod]
	public void AddFrontmatter_DoubleNotExactInFloat32_KeepsTheHeaderAndAppendsTheKey()
	{
		string result = Frontmatter.AddFrontmatter(Input, new Dictionary<string, object> { ["lat"] = 51.507351 });

		Assert.AreEqual("---\n# Venue page; do not publish until confirmed\ntitle: Venue\npublished: no\nlat: 51.507351\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_DecimalWithTrailingZero_KeepsTheHeaderAndAppendsTheKey()
	{
		string result = Frontmatter.AddFrontmatter(Input, new Dictionary<string, object> { ["price"] = 9.90m });

		Assert.AreEqual("---\n# Venue page; do not publish until confirmed\ntitle: Venue\npublished: no\nprice: 9.90\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_DoubleExactInFloat32_StillKeepsTheHeader()
	{
		string result = Frontmatter.AddFrontmatter(Input, new Dictionary<string, object> { ["lat"] = 51.5 });

		Assert.AreEqual("---\n# Venue page; do not publish until confirmed\ntitle: Venue\npublished: no\nlat: 51.5\n---\nBody\n", result);
	}
}
