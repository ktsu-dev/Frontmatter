// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for the process-wide caches, which used to keep every document, frontmatter block
/// and property name ever processed until the process exited.
/// </summary>
[TestClass]
public class BoundedCacheTests
{
	[TestMethod]
	public void Add_PastCapacity_KeepsAtMostCapacityEntriesAndTheNewestOne()
	{
		BoundedCache<int, int> cache = new(capacity: 3);

		for (int i = 0; i < 10; i++)
		{
			cache.Add(i, i * 2);
		}

		Assert.IsLessThanOrEqualTo(3, cache.Count);
		Assert.IsTrue(cache.TryGetValue(9, out int value));
		Assert.AreEqual(18, value);
	}

	[TestMethod]
	public void CombineFrontmatter_ManyDistinctDocuments_CachesStayWithinTheirCapacity()
	{
		int documents = YamlSerializer.ParsedYamlCache.Capacity + 100;

		for (int i = 0; i < documents; i++)
		{
			_ = Frontmatter.CombineFrontmatter($"---\ntitle: Document {i}\nkey{i}: value\n---\nbody {i}\n");
		}

		Assert.IsLessThanOrEqualTo(Frontmatter.ProcessedFrontmatterCache.Capacity, Frontmatter.ProcessedFrontmatterCache.Count);
		Assert.IsLessThanOrEqualTo(YamlSerializer.ParsedYamlCache.Capacity, YamlSerializer.ParsedYamlCache.Count);
	}

	[TestMethod]
	public void CombineFrontmatter_ManyDistinctPropertyNames_NameCacheStaysWithinItsCapacity()
	{
		int capacity = NameStandardizer.PropertyNameCache.Capacity;

		for (int i = 0; i < capacity + 100; i += 20)
		{
			string keys = string.Join("\n", Enumerable.Range(i, 20).Select(k => $"custom{k}: v"));
			_ = Frontmatter.CombineFrontmatter($"---\n{keys}\n---\nbody\n");
		}

		Assert.IsLessThanOrEqualTo(capacity, NameStandardizer.PropertyNameCache.Count);
	}

	[TestMethod]
	public void CombineFrontmatter_SameDocumentTwice_StillReturnsTheSameResult()
	{
		const string input = "---\nauthor: A\n---\n---\ntitle: T\n---\nbody\n";

		Assert.AreEqual(Frontmatter.CombineFrontmatter(input), Frontmatter.CombineFrontmatter(input));
	}
}
