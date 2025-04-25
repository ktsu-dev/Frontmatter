# ktsu.Frontmatter

A .NET library for processing and manipulating YAML frontmatter in markdown files.

## Features

- Extract, add, replace, and remove frontmatter from markdown documents
- Combine multiple frontmatter sections into a single section
- Standardize property names using intelligent matching
- Sort properties according to standard conventions
- Merge similar properties using different strategies
- High performance with caching for repeated operations

## Installation

```bash
dotnet add package ktsu.Frontmatter
```

## Usage

```csharp
using ktsu.Frontmatter;

// Extract frontmatter from a markdown document
string markdown = File.ReadAllText("document.md");
var frontmatter = Frontmatter.ExtractFrontmatter(markdown);

// Add frontmatter to a document
var properties = new Dictionary<string, object>
{
    { "title", "My Document" },
    { "date", DateTime.Now },
    { "tags", new[] { "documentation", "markdown" } }
};
string withFrontmatter = Frontmatter.AddFrontmatter(markdown, properties);

// Replace frontmatter
string replaced = Frontmatter.ReplaceFrontmatter(markdown, properties);

// Remove frontmatter
string withoutFrontmatter = Frontmatter.RemoveFrontmatter(markdown);

// Combine multiple frontmatter sections
string combined = Frontmatter.CombineFrontmatter(markdown);

// Customize property naming and ordering
string customized = Frontmatter.CombineFrontmatter(
    markdown, 
    FrontmatterNaming.Standard,  // Standardize property names
    FrontmatterOrder.Sorted,     // Sort properties in standard order
    FrontmatterMergeStrategy.Conservative // Merge similar properties
);

// Extract just the document body (content after frontmatter)
string body = Frontmatter.ExtractBody(markdown);
```

## Advanced Features

### Property Naming

Control how property names are handled:

- `FrontmatterNaming.AsIs`: Keep property names as-is
- `FrontmatterNaming.Standard`: Standardize property names using common conventions

### Property Ordering

Control how properties are ordered:

- `FrontmatterOrder.AsIs`: Keep properties in the order they appear
- `FrontmatterOrder.Sorted`: Sort properties according to standard conventions

### Merge Strategies

Control how similar properties are merged:

- `FrontmatterMergeStrategy.None`: Do not merge any properties
- `FrontmatterMergeStrategy.Conservative`: Only merge properties using predefined mappings
- `FrontmatterMergeStrategy.Aggressive`: Merge properties using basic pattern matching
- `FrontmatterMergeStrategy.Maximum`: Merge properties using semantic analysis

## License

MIT
