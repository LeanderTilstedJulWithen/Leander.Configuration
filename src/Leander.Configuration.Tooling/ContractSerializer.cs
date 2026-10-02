using System.Text.Json;
using Leander.Configuration.Descriptors;
using Leander.Configuration.Tooling.Internal;

namespace Leander.Configuration.Tooling;

/// <summary>
/// Writes a contract descriptor and reads it back, e.g. to commit it as a contract file.
/// </summary>
/// <remarks>
/// The written contract is descriptive: it can be committed and compared, but never run,
/// because normalizers and validators are code.
/// </remarks>
public static class ContractSerializer
{
    /// <summary>
    /// The version of the JSON format this library writes and reads.
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// Reads a contract descriptor from JSON written by <see cref="WriteJson"/>.
    /// </summary>
    /// <exception cref="JsonException">The JSON is malformed, misses a required property, or has an unknown value.</exception>
    /// <exception cref="FormatException">The JSON is empty, or its format version isn't <see cref="FormatVersion"/>.</exception>
    public static ContractDescriptor ReadJson(string json) => JsonContract.Read(json);

    /// <summary>
    /// Writes <paramref name="contract"/> as indented JSON, with the format version.
    /// </summary>
    public static string WriteJson(ContractDescriptor contract) => JsonContract.Write(contract);
}
