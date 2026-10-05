using System.Security.Cryptography;
using DotNetSchema.Generator;

namespace DotNetSchema.Generator.Tests;

/// <summary>
/// Reduces a document to one hash, so that drift anywhere in it lands on a single assertion.
/// </summary>
/// <remarks>
/// The same idea as <c>FixtureDigest</c> in the fixtures test suite, and for the same reason — but far
/// simpler, because a document is already a canonical string. There is no rendering to do here: the
/// generator's own output is the thing being pinned, byte for byte, including its trailing newline.
/// </remarks>
public static class SchemaDigest
{
  /// <summary>The SHA-256 of a document's bytes, lowercase hex.</summary>
  public static string Of(SchemaDocument document) =>
    Convert.ToHexStringLower(SHA256.HashData(document.ToUtf8Bytes()));
}
