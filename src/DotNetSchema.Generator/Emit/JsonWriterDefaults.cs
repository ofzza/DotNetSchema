using System.Text.Encodings.Web;
using System.Text.Json;

namespace DotNetSchema.Generator.Emit;

/// <summary>How every schema document is written. Each setting here is load-bearing for byte stability.</summary>
public static class JsonWriterDefaults
{
  /// <summary>The writer options every document is produced with.</summary>
  public static JsonWriterOptions Options { get; } = new()
  {
    Indented = true,
    IndentCharacter = ' ',
    IndentSize = 2,

    // Must be set. JsonWriterOptions.NewLine defaults to Environment.NewLine, so an unset writer emits
    // CRLF on Windows and LF everywhere else — which makes the same input produce two different files
    // and puts a golden value out of reach.
    NewLine = "\n",

    // The default encoder escapes '+', '<', '>', '&' and '\'' as \uXXXX, which would turn the TimeSpan
    // pattern's \d+ into \d+: still valid JSON, but unreadable in a review and gratuitously
    // different from what every other tool writes. "Unsafe" here means unsafe to interpolate into HTML,
    // which a schema file on disk is not.
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

    SkipValidation = false,
  };
}
