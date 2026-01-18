# Pkgdef Language Error Reference

This document provides detailed information about validation errors in the Pkgdef Language extension.

---

## PL001

**Description:** An unrecognized token was found in the document. This could be malformed syntax, a typo, or an unsupported construct.

**Triggered by:**
- Random text that doesn't match any valid pkgdef syntax
- Misplaced characters or symbols
- Text outside of registry key declarations or comments

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
SomeRandomText
"ValidProperty"="Value"
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
; SomeRandomText (comment added)
"ValidProperty"="Value"
```

---

## PL002

**Description:** A registry key declaration is missing its closing bracket (`]`). Every registry key must start with `[` and end with `]`.

**Triggered by:**
- Missing the closing `]` character in a registry key declaration
- Line breaks before the closing bracket

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage
"Property"="Value"
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"Property"="Value"
```

---

## PL003

**Description:** Registry paths must use backslashes (`\`) as delimiters, not forward slashes (`/`). This is a Windows registry requirement.

**Triggered by:**
- Using `/` instead of `\` in registry key paths
- Copy-pasting Unix-style paths

**Example - Before:**
```pkgdef
[$RootKey$/MyPackage/SubKey]
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage\SubKey]
```

---

## PL004

**Description:** The default value property should be represented by an unquoted `@` symbol. When quotes are used, it's treated as a named property called "@" instead of the default value.

**Triggered by:**
- Wrapping the `@` symbol in quotation marks

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
"@"="Default Value"
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
@="Default Value"
```

---

## PL005

**Description:** Property names must be enclosed in double quotation marks, except for the special `@` default value indicator.

**Triggered by:**
- Property names without quotes
- Partial quotes (only opening or closing quote)
- Single quotes instead of double quotes

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
MyProperty="Value"
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"MyProperty"="Value"
```

---

## PL006

**Description:** A variable is referenced but doesn't exist in the predefined variables list. Common variables include `$RootKey$`, `$PackageFolder$`, `$RootFolder$`, etc.

**Triggered by:**
- Typos in variable names (case-sensitive)
- Using undefined custom variables
- Missing variable from the predefined list

**Example - Before:**
```pkgdef
[$RootKy$\MyPackage]  ; Typo: RootKy instead of RootKey
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
```

**Available Variables:**
- `$AppDataLocalFolder$`
- `$ApplicationExtensionsFolder$`
- `$AppName$`
- `$BaseInstallDir$`
- `$CommonFiles$`
- `$Initialization$`
- `$MyDocuments$`
- `$PackageFolder$`
- `$ProgramFiles$`
- `$RootFolder$`
- `$RootKey$`
- `$ShellFolder$`
- `$System$`
- `$WinDir$`

---

## PL007

**Description:** Variables must begin and end with the dollar sign (`$`) character. A variable without proper delimiters won't be recognized.

**Triggered by:**
- Missing opening `$`
- Missing closing `$`
- Using other characters as variable delimiters

**Example - Before:**
```pkgdef
[RootKey$\MyPackage]  ; Missing opening $
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
```

---

## PL008

**Description:** The same registry key path is defined multiple times in the document. While not strictly an error, this can lead to confusion and unexpected behavior.

**Triggered by:**
- Declaring the same registry key path more than once
- Copy-pasting registry key sections

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
"Property1"="Value1"

[$RootKey$\MyPackage]
"Property2"="Value2"
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"Property1"="Value1"
"Property2"="Value2"
```

---

## PL009

**Description:** DWORD (32-bit) registry values must be exactly 8 hexadecimal characters (0-9, A-F). This represents a 32-bit unsigned integer.

**Triggered by:**
- Too few hex characters (e.g., `dword:7b`)
- Too many hex characters (e.g., `dword:123456789`)
- Invalid characters (e.g., `dword:GGGGGGGG`)
- Missing hex value after `dword:`

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=dword:7b           ; Too short
"Value2"=dword:123456789    ; Too long
"Value3"=dword:ZZZZZZZZ     ; Invalid characters
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=dword:0000007b
"Value2"=dword:12345678
"Value3"=dword:00000000
```

---

## PL010

**Description:** QWORD (64-bit) registry values must be exactly 16 hexadecimal characters (0-9, A-F). This represents a 64-bit unsigned integer.

**Triggered by:**
- Too few hex characters (e.g., `qword:7b`)
- Too many hex characters
- Invalid characters
- Missing hex value after `qword:`

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=qword:7b                 ; Too short
"Value2"=qword:12345678901234567  ; Too long
"Value3"=qword:GGGGGGGGGGGGGGGG  ; Invalid characters
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=qword:000000000000007b
"Value2"=qword:1234567890123456
"Value3"=qword:0000000000000000
```

---

## PL011

**Description:** HEX array values must be comma-separated 2-digit hexadecimal bytes. Each byte is represented by exactly 2 hex characters (00-FF).

**Triggered by:**
- Bytes not separated by commas
- Bytes with incorrect length (not 2 characters)
- Invalid hex characters
- Missing value after `hex:` or `hex(X):`

**Example - Before:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=hex:01020304        ; Missing commas
"Value2"=hex:1,2,3,4         ; Each byte needs 2 digits
"Value3"=hex:GG,HH,II        ; Invalid characters
```

**Example - After:**
```pkgdef
[$RootKey$\MyPackage]
"Value1"=hex:01,02,03,04
"Value2"=hex:01,02,03,04
"Value3"=hex:00,00,00
```

**Note:** The prefix can be `hex:` for REG_BINARY or `hex(X):` where X is a number indicating the registry type (e.g., `hex(2):` for REG_EXPAND_SZ, `hex(7):` for REG_MULTI_SZ).
