# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

- `dotnet build` - Build the solution
- `dotnet test` - Run all tests
- `dotnet test --filter "FullyQualifiedName~TestName"` - Run specific test

## Architecture

This is a .NET library for processing YAML frontmatter in markdown files. The library uses ktsu.Sdk custom SDK packages which handle multi-targeting and common project settings.

### Core Components

**Frontmatter.cs** - Main static class with public API:
- `CombineFrontmatter()` - Merges multiple frontmatter sections into one
- `ExtractFrontmatter()` / `ExtractBody()` - Parse frontmatter and content separately
- `AddFrontmatter()` / `ReplaceFrontmatter()` / `RemoveFrontmatter()` - Modify documents
- Uses `ConcurrentDictionary` caching with FNV-1a hashing for performance

**Processing Pipeline** (used by `CombineFrontmatter`):
1. **PropertyMerger** - Merges similar properties based on `FrontmatterMergeStrategy` (None/Conservative/Aggressive/Maximum)
2. **NameStandardizer** - Maps non-standard property names to canonical names using `PropertyMappings`
3. **StandardOrder** - Sorts properties by predefined category order (core metadata, dates, authors, categorization, etc.)

**PropertyMappings.cs** - Immutable dictionaries mapping variant property names (e.g., "creator", "written-by") to canonical names (e.g., "author"). Categories: Title, Author, Date, Tags, Categories, Description, Modified, Layout, Permalink.

**YamlSerializer.cs** - Wrapper around YamlDotNet with caching and deep-clone support for parsed results.

### Key Dependencies

- **YamlDotNet** - YAML parsing/serialization
- **HashDepot** - FNV-1a hashing for cache keys
- **ktsu.Extensions** - String extension methods
- **ktsu.FuzzySearch** - Fuzzy matching (referenced but main matching uses PropertyMappings)
