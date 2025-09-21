using System;
using System.Text.Json.Serialization;

namespace Altinn.Register.Models;

/// <summary>
/// Represents a party in Altinn Register.
/// </summary>
public class Party
{
    /// <summary>
    /// Gets the type of the party.
    /// </summary>
    [JsonPropertyName("partyType")]
    public string Type { get; }

    /// <summary>
    /// Gets the type of the party.
    /// </summary>
    [JsonPropertyName("personIdentifier")]
    public string Pid { get; }

    /// <summary>
    /// Gets the UUID of the party.
    /// </summary>
    [JsonPropertyName("partyUuid")]
    public Guid Uuid { get; init; }

    /// <summary>
    /// Gets the version ID of the party.
    /// </summary>
    [JsonPropertyName("versionId")]
    public ulong VersionId { get; init; }

    /// <summary>
    /// Gets the canonical URN of the party.
    /// </summary>
    [JsonPropertyName("urn")]
    public string Urn { get; init; }

    /// <summary>
    /// Gets the ID of the party.
    /// </summary>
    [JsonPropertyName("partyId")]
    public uint PartyId { get; init; }

    /// <summary>
    /// Gets the user ID of the party.
    /// </summary>
    [JsonPropertyName("userId")]
    public uint UserId { get; init; }

    /// <summary>
    /// Gets the display-name of the party.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; }
}