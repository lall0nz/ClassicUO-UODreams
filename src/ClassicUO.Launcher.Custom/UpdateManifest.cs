using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Launcher.Custom
{
    internal sealed class UpdateManifest
    {
        public const int SupportedSchemaVersion = 1;
        public const string ManifestFileName = "update.json";

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; }

        [JsonPropertyName("edition")]
        public string Edition { get; init; } = "";

        [JsonPropertyName("releaseTag")]
        public string ReleaseTag { get; init; } = "";

        [JsonPropertyName("publishedAt")]
        public string? PublishedAt { get; init; }

        [JsonPropertyName("notes")]
        public UpdateManifestNotes? Notes { get; init; }

        [JsonPropertyName("components")]
        public Dictionary<string, UpdateManifestComponent>? Components { get; init; }

        public static UpdateManifest? TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public bool IsSupportedForCurrentEdition()
        {
            if (SchemaVersion != SupportedSchemaVersion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Edition))
            {
                return false;
            }

            return Edition.Equals(LauncherManifest.Edition, StringComparison.OrdinalIgnoreCase);
        }

        public UpdateManifestComponent? GetComponent(string name)
        {
            if (Components == null || !Components.TryGetValue(name, out UpdateManifestComponent? component))
            {
                return null;
            }

            return component.IsValid ? component : null;
        }

        public string GetLocalizedNotes(string? fallback = null)
        {
            string? localized = Loc.IsEn ? Notes?.En : Notes?.It;
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized.Trim();
            }

            localized = Loc.IsEn ? Notes?.It : Notes?.En;
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized.Trim();
            }

            return fallback ?? "";
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    internal sealed class UpdateManifestNotes
    {
        [JsonPropertyName("it")]
        public string? It { get; init; }

        [JsonPropertyName("en")]
        public string? En { get; init; }
    }

    internal sealed class UpdateManifestComponent
    {
        [JsonPropertyName("version")]
        public string Version { get; init; } = "";

        [JsonPropertyName("asset")]
        public string Asset { get; init; } = "";

        [JsonPropertyName("sha256")]
        public string Sha256 { get; init; } = "";

        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; init; }

        [JsonPropertyName("notes")]
        public UpdateManifestNotes? Notes { get; init; }

        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Version) &&
            !string.IsNullOrWhiteSpace(Asset) &&
            Asset.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

        public string GetLocalizedNotes()
        {
            string? localized = Loc.IsEn ? Notes?.En : Notes?.It;
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized.Trim();
            }

            localized = Loc.IsEn ? Notes?.It : Notes?.En;
            return localized?.Trim() ?? "";
        }

        public string BuildDownloadUrl(string releaseTag) =>
            $"https://github.com/{LauncherManifest.GitHubRepo}/releases/download/{releaseTag}/{Asset}";
    }
}
