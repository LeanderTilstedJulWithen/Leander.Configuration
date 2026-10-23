; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
LCFG001 | Leander.Configuration | Error | Invalid options method
LCFG002 | Leander.Configuration | Error | Containing type is not partial
LCFG003 | Leander.Configuration | Error | Several constructors with FromDefinition parameters
LCFG004 | Leander.Configuration | Error | Constructor parameter without FromDefinition
LCFG005 | Leander.Configuration | Error | No constructor to use
LCFG006 | Leander.Configuration | Error | Property can't be set
LCFG007 | Leander.Configuration | Error | Definition not found
LCFG008 | Leander.Configuration | Error | Not a configuration definition
LCFG009 | Leander.Configuration | Error | Definition is inaccessible
LCFG010 | Leander.Configuration | Error | Definition type doesn't match
LCFG011 | Leander.Configuration | Warning | Optional definition for a non-nullable member
