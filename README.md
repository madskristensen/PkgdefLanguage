[marketplace]: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.PkgdefLanguage
[vsixgallery]: http://vsixgallery.com/extension/06278dd5-5d9d-4f27-a3e8-cd619b101a50/
[repo]:https://github.com/madskristensen/PkgdefLanguage/

# Pkgdef Language for Visual Studio

[![Build](https://github.com/madskristensen/PkgdefLanguage/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/PkgdefLanguage/actions/workflows/build.yaml)

Download this extension from the [Visual Studio Marketplace][marketplace]
or get the [CI build][vsixgallery].

--------------------------------------

A comprehensive language service for `.pkgdef` and `.pkgundef` files that brings modern IDE features to Visual Studio extension development. Write registry configurations faster, catch errors before they cause problems, and keep your code clean with automatic formatting and refactoring.

## Features at a Glance

| Feature | Description |
|---------|-------------|
| 🎨 **Syntax Highlighting** | Color-coded elements for easy reading |
| 💡 **IntelliSense** | Smart completions for variables, registry keys, and values |
| ⚠️ **Validation** | 11 different error checks with detailed messages |
| 🔧 **Quick Fixes** | One-click fixes for common errors |
| ✨ **Refactoring** | Sort properties and add default values |
| 📁 **Outlining** | Collapse sections for better overview |
| 📝 **Formatting** | Consistent code style with one keystroke |

## Syntax Highlighting

Color-coded syntax makes it easy to parse your documents at a glance. Registry keys, property names, values, variables, and comments are all distinctly styled.

![Colorization](art/colorization.png)

## IntelliSense

Rich completions throughout your `.pkgdef` files to speed up development and reduce errors:

| Context | What You Get |
|---------|--------------|
| **Variables** | Type `$` to see all predefined variables (`$RootKey$`, `$PackageFolder$`, `$RootFolder$`, etc.) |
| **Registry Keys** | Path suggestions based on your current location in the registry hive |
| **Property Names** | Common property names after defining a registry key |
| **Property Values** | Value types (`dword:`, `qword:`, `hex:`), strings, GUIDs, and more |

![IntelliSense](art/intellisense.gif)

## Validation

Comprehensive validation catches mistakes before they cause runtime issues:

| Category | Checks |
|----------|--------|
| **Syntax** | Unclosed brackets, missing quotes, forward slashes in paths, unknown tokens |
| **Variables** | Undefined variables, missing `$` delimiters, typos in variable names |
| **Registry Values** | DWORD (8 hex chars), QWORD (16 hex chars), HEX arrays (comma-separated bytes) |
| **Semantics** | Duplicate registry keys, quoted `@` for default values |

For detailed information about each validation error, including examples and fixes, see the [Error Reference](errors.md).

![Validation](art/validation.png)

## Quick Fixes

Press `Ctrl+.` on any error to see available one-click fixes:

| Error | Quick Fix |
|-------|-----------|
| **PL002** - Unclosed registry key | Add closing `]` |
| **PL003** - Forward slashes | Convert `/` to `\` |
| **PL004** - Quoted @ sign | Remove quotes (`"@"` → `@`) |
| **PL005** - Unquoted property name | Surround with quotes |
| **PL005** - Unclosed string | Add closing `"` |
| **PL006** - Unknown variable | Suggest similar variable name |
| **PL007** - Unclosed variable | Add closing `$` |
| **PL008** - Duplicate registry key | Consolidate all properties under first occurrence |

![Code Fix](art/code-fix.png)

## Refactoring

Beyond error fixes, the extension offers refactoring actions to improve your code:

- **Add default value** - Insert `@=""` for registry keys missing a default value
- **Sort properties** - Alphabetically sort properties with `@` always first

Access refactoring actions with `Ctrl+.` when your cursor is on a registry key.

## Quick Info

Hover over elements for contextual information:

- **Variables** - See descriptions and resolved values for predefined variables like `$RootKey$`
- **Errors** - View detailed error messages with clickable error codes that link to documentation

![Quick Info](art/quick-info.png)

## Outlining

Collapse registry key sections to get a better overview of large documents. Each registry key with its properties forms a collapsible region.

![Outlining](art/outlining.png)

> **Note:** Only comments starting with a semicolon (`;`) are correctly identified as comments, not those starting with `//`.

## Formatting

Format your entire document with `Ctrl+K, Ctrl+D` or just a selection with `Ctrl+K, Ctrl+F`:

- Adds consistent line breaks between registry key entries
- Trims unnecessary whitespace
- Ensures consistent indentation

![Formatting](art/formatting.png)

---

## Contributing

If you enjoy using the extension, please give it a ★★★★★ rating on the [Visual Studio Marketplace][marketplace].

Found a bug or have a feature request? Head over to the [GitHub repo][repo] to open an issue.

Pull requests are welcome! This is a personal passion project, so contributions help keep it moving forward.

You can also [sponsor me on GitHub](https://github.com/sponsors/madskristensen) to support ongoing development.