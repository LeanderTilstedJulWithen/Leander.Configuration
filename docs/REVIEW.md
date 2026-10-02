# Review

This is a review of the current state of the code.
Last update 2026/10/02

## Contract

### Contract export formatting
Currently when exporting a contract you get something like

```json
{
    "key": "Server:Port",
    "description": "The port to listen on.",
    "isSensitive": false,
    "value": {
    "type": "Int32",
    "presence": "default",
    "form": "scalar",
    "default": "8080",
    "primitive": {
        "type": "Int32",
        "name": "Port"
    }
    }
}
```

Which has some repetitive and redundant information. I think a clearer format is:

```json
{
    "key": "Server:Port",
    "description": "The port to listen on.",
    "type": "Int32"
    "primitive": "Port"
    "isSensitive": false,
    "presence": "default",
    "default": "8080",
    "form": "scalar",
}
```

Optionally we could trim even further down to:

```json
{
    "key": "Server:Port",
    "description": "The port to listen on.",
    "type": "Int32"
    "primitive": "Port"
    "presence": "default",
    "default": "8080",
}
```

since "isSensitive": false is the default. Also "form": "scalar" is the default for most values, we might omit it. The only place where it actually is useful is something like:

```json
{
    "key": "Server:Features",
    "description": "Comma-separated list of enabled features.",
    "isSensitive": false,
    "value": {
    "type": "IReadOnlyList<String>",
    "presence": "required",
    "form": "scalar",
    "primitive": {
        "type": "IReadOnlyList<String>",
        "name": "Features"
    }
    }
},
```

where the explicit "scalar" tells that this is read as a scalar even though the type is list. I think this is reason enough to keep scalar. But "isSensitive": false, could surely be removed. Remark: isSensitive looks akward when compared to other names, since it is the only 2-word key.

### The JSON problem
Formatting using json implies, that changing the name or structure of any descriptor silently breaks the formatting. This needs to either be explictly tested (like have a full contract-file (as string) to compare the export to). 

Alternatively (recommended): The tooling owns the format. One way to do this: The tooling can have its own class ContractJsonExport (or similar name, JSON dto or whatever). Then the only way to change the format is to explicitly change this class. This also means we don't need explicit CamelCasing rules on JSONoptions, we can have a single file controlling the format. The simpler the JSON parser options the simpler it is to reason about.

### Naming
I believe ContractFile.Write should be ContractFile.WriteJson or ContractFile.Write(contract, options), so that we are explicit about the format, and we are open towards adding other formats later.

Also, in C# Comparison means something like GreaterThan or LesserThanOrEqual to, so the wording ContractComparison clashes with C# conventions. 

### Who owns descriptors (Open question)
Currently the main project Leander.Configuration owns the descriptors, but it is mainly used by tooling. Is this problematic? probably not. It does add a bit of noise, because every class needs to know how to create the descriptor.

### Markdown Output
#### Show in documentation
The markdown output is nice. It is also one of the main selling points of the project. It needs to be sold explicitely in the documentation/readme. We could have a samples/output or samples/artifacts, which has example output. The sample output could then be referenced from the readme.

#### ConfigurationValues vs. Primitive split
While the output is good, it could be better. The split between configurationdefinition and their primitive makes it hard to read. You need to go to the primitive section to see how a value is validated, so it likely makes better sense to show the validation there on the object. Also i am not sure lists show as they should, since it has a sort of nested validation. 

#### Too little information on the primitive
Most primitives have too little information. Some might say derived from Int32, but Int32 isn't used because it isn't used explicitly. A primitive needs to also say how it is parsed. Int32 might be obvious but there is no hurt in being explicit. "An integer value between -2,147,483,648 and 2,147,483,647. Parsing uses invariant culture." (or similar). Also it may have links to values that use it, so you can say "Port: Used by: "Server:Port" (link)

## General

### Coding style
I think a class should look something like:

Use primary constructor when possible.
private static fields
private readonly fields (ideally read from primary constructor)
private fields
constructors (if any. public, then internal, then private)
public static members
public Properties
Internal Properties
Public methods
Internal methods
Private methods
Nested Classes

In addition we favor alphabetic order.

## Leander.Configuration.MicrosoftExtensions

### Rethink API
The current extension methods forces: Immediate throw if contract is not satisfied. This goes against the typical way DI is setup. No errors should be thrown before build is called. I believe we need an options parameter: To customize setup. The options parameter can then be extended later to allow IOptionsMonitor support.

Also we need to be careful about convention vs. rules. We decided upon SingleTon registration, but this might not suit every need.

## README.md

### Tightness
The README file used to be tight and well-formulated, but it has changed a bit a long with added feature. Some formulations are a bit "clunky", like: "A primitive is always explicit and complete, with a name and a converter: sharing one is sharing a field." What does that even mean? 

### Why
Having a section justifiing the Library is great. Should it explicitely be named "Why" though? Is there a better name? Overall this section is good, but it could add a bit more information about the added features, but without a full walkthrough.

### Leander.Primitives
The intro is still spot on: "An `int` is not a port, and a `string` is not an e-mail address. A primitive gives the same underlying type its own rules, without a wrapper type:". This is great! I still don't like the formulation: "A primitive is always explicit and complete, with a name and a converter: sharing one is sharing a field.". The first example is simple and great. The list example is good. I think we could show the functionality in more depth. Perhaps we should have a new project under \samples which demonstrates the standalone functionality of Primitives.

### Leander.Configuration
This section is fine, but it should really mention Microsoft.Extensions, since we have a seperate project which handles that. Anything related to IConfiguraiont and Dependency injection could move to its own section.

### Documentation and ContractFiles
The section name should probably reflect the project name as the other sections do. This section currently mixes a lot of responsibility. It should just name what the project does.

### Samples
We should create a new section samples, which links to all samples. Including output samples from like documentation.md

### Planned
A small section for planned additions. Note: this should NOT be the same as TODO.md nor DESIGN.md (those are for internal use). These are things we have explicively planned, such as eventual coverage of IOptionsMonitor.

## Samples
### More samples
I think we need more samples: I believe one sample pr. project + a sample of each generated output(configuration.contract.json, configuration.documentaion.md, and appsettings.json) + a sample which covers everything.

