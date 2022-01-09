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

## Outlining
Collapse sections for better overview of the document.

![Outlining](art/outlining.png)

Notice how only comments starting with a semicolon is correctly identified as a comment.

## Formatting
You can format the whole document `Ctrl+K,Ctrl+D` or the current selection `Ctrl+K,Ctrl+F`. It will add a line break between registry key entries, trim whitespace, and other clean-up formatting.

![Formatting](art/formatting.png)

### How can I help?
If you enjoy using the extension, please give it a ★★★★★ rating on the [Visual Studio Marketplace][marketplace].

Should you encounter bugs or if you have feature requests, head on over to the [GitHub repo][repo] to open an issue if one doesn't already exist.

Pull requests are also very welcome, since I can't always get around to fixing all bugs myself. This is a personal passion project, so my time is limited.

Another way to help out is to [sponser me on GitHub](https://github.com/sponsors/madskristensen).