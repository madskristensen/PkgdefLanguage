[marketplace]: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.PkgdefLanguage
[vsixgallery]: http://vsixgallery.com/extension/06278dd5-5d9d-4f27-a3e8-cd619b101a50/
[repo]:https://github.com/madskristensen/PkgdefLanguage/

# Pkgdef Language for Visual Studio

[![Build](https://github.com/madskristensen/PkgdefLanguage/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/PkgdefLanguage/actions/workflows/build.yaml)

Download this extension from the [Visual Studio Marketplace][marketplace]
or get the [CI build][vsixgallery].

--------------------------------------

This extension provides basic language support for .pkgdef and .pkgundef files in Visual Studio.

## Syntax highlighting
Syntax highlighting makes it easy to parse the document. Here's what it looks like:

![Colorization](art/colorization.png)

## IntelliSense
Full completion provided for variables and registry keys.

![IntelliSense](art/intellisense.gif)

## Validation
There's validation for both syntax errors, unknown variables, and more to help you avoid mistakes.

![Validation](art/validation.png)

### Quick Fixes
The extension provides automatic code fixes for common errors. Simply press `Ctrl+.` on any error to see available fixes:

<!-- TODO: Add screenshot of lightbulb quick fixes in action -->

**Available Quick Fixes:**
- **Missing closing bracket** - Automatically adds the missing `]` to registry keys
- **Forward slashes in paths** - Converts `/` to `\` in registry paths
- **Quoted @ sign** - Removes quotes from default value property (`"@"` → `@`)
- **Unquoted property names** - Surrounds property names with required quotation marks

### Value Type Validation
Registry property values are validated to ensure correct format:

<!-- TODO: Add screenshot showing dword/qword/hex validation errors -->

- **DWORD values** (`dword:`) - Must be exactly 8 hexadecimal characters
  - ✅ Valid: `dword:0000007b`
  - ❌ Invalid: `dword:7b` (too short)
  - ❌ Invalid: `dword:GGGGGGGG` (invalid characters)

- **QWORD values** (`qword:`) - Must be exactly 16 hexadecimal characters
  - ✅ Valid: `qword:00000000ffffffff`
  - ❌ Invalid: `qword:00000000` (too short)

- **HEX arrays** (`hex:` or `hex(X):`) - Must be comma-separated 2-digit hex bytes
  - ✅ Valid: `hex:01,02,03,04,ff`
  - ❌ Invalid: `hex:0102,03,04` (wrong format)
  - ❌ Invalid: `hex:GG,02,03` (invalid characters)

### Validation Error Codes

| Code  | Description                    | Severity      | Has Quick Fix |
|-------|--------------------------------|---------------|---------------|
| PL001 | Unknown token                  | Error         | ❌            |
| PL002 | Unclosed registry key          | Error         | ✅            |
| PL003 | Forward slash in path          | Error         | ✅            |
| PL004 | Quoted @ sign                  | Warning       | ✅            |
| PL005 | Missing quotes on property     | Error         | ✅            |
| PL006 | Unknown variable               | Warning       | ❌            |
| PL007 | Invalid variable syntax        | Error         | ❌            |
| PL008 | Duplicate registry key         | Info          | ❌            |
| PL009 | Invalid dword value            | Error         | ❌            |
| PL010 | Invalid qword value            | Error         | ❌            |
| PL011 | Invalid hex array value        | Error         | ❌            |

## Outlining
Collapse sections for better overview of the document.

![Outlining](art/outlining.png)

Notice how only comments starting with a semicolon is correctly identified as a comment.

## Formatting
You can format the whole document `Ctrl+K,Ctrl+D` or the current selection `Ctrl+K,Ctrl+F`. It will add a line break between registry key entries, trim whitespace, and other clean-up formatting.

![Formatting](art/formatting.png)

## Testing Quick Fixes

To test the new quick fix features, open the included `test-quickfixes.pkgdef` file in Visual Studio. Place your cursor on any error (red squiggle) and press `Ctrl+.` to see available quick fixes. Each section demonstrates a different type of validation error and its corresponding fix.

<!-- TODO: Add screenshot showing test file with various errors -->

---

### How can I help?
If you enjoy using the extension, please give it a ★★★★★ rating on the [Visual Studio Marketplace][marketplace].

Should you encounter bugs or if you have feature requests, head on over to the [GitHub repo][repo] to open an issue if one doesn't already exist.

Pull requests are also very welcome, since I can't always get around to fixing all bugs myself. This is a personal passion project, so my time is limited.

Another way to help out is to [sponsor me on GitHub](https://github.com/sponsors/madskristensen).