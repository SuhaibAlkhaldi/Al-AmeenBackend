using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DLPManagementSystem.Service.Service
{
    // Verifies SecurityEventEnvelope.IntegrityHash against the RAW JSON bytes as received over the
    // wire, not a reconstructed/re-typed DTO. This deliberately sidesteps the DTO-parity bug class that
    // bit the policy-signing fix once already (see AgentPolicyResultDto's comments): backend DTOs for
    // this envelope currently disagree with CompanyDlp.Contracts.SecurityEventEnvelope on several
    // fields' nullability/defaults, which would silently break a reconstructed-object hash check for
    // every legitimate event.
    //
    // Blanking is done via an exact substring replacement on the original raw text, NOT by parsing into
    // a JsonNode and re-serializing it - that was the first approach tried here, and it fails for every
    // real event, not just tampered ones: CompanyDlp.Service.SecurityEventFactory.ComputeIntegrityHash
    // serializes the envelope as a TYPED object, and System.Text.Json's built-in DateTimeOffset
    // converter writes its value (e.g. "...+00:00") through a native fast path that never runs it past
    // the configured string encoder. Once that same text is parsed back into a JsonNode, the offset
    // becomes a plain string value, and re-serializing it DOES go through the encoder - which escapes
    // '+' to "+" by default. Same options on both sides, still a guaranteed byte mismatch, because
    // the divergence is about which code path a value takes, not which options are configured. A
    // targeted substring replacement never re-serializes anything, so this never comes up: every byte
    // of the original request other than the hash's own value is preserved exactly.
    public static class AuditIntegrityVerifier
    {
        // null = no hash present to check (missing/blank - an older agent, or a field that was never
        // populated; not itself suspicious). true = recomputed hash matches. false = a hash was present
        // but didn't match, or was malformed (wrong length/not hex) - either way, flagged for review.
        public static bool? Verify(JsonElement eventElement)
        {
            if (!eventElement.TryGetProperty("integrityHash", out var hashProperty)
                || hashProperty.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var suppliedHash = hashProperty.GetString();
            if (string.IsNullOrEmpty(suppliedHash))
            {
                return null;
            }

            if (suppliedHash.Length != 64 || !suppliedHash.All(Uri.IsHexDigit))
            {
                return false;
            }

            byte[] suppliedHashBytes;
            try
            {
                suppliedHashBytes = Convert.FromHexString(suppliedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            // A hex string never contains a character JSON needs to escape, so this literal is
            // guaranteed to appear in the raw text verbatim (the agent always sends compact,
            // non-indented JSON - see CompanyDlp.Contracts.JsonDefaults.Options - so there is no
            // whitespace between the property name, colon, and value to account for either).
            var rawText = eventElement.GetRawText();
            var hashLiteral = $"\"integrityHash\":\"{suppliedHash}\"";
            var hashLiteralIndex = rawText.IndexOf(hashLiteral, StringComparison.Ordinal);
            if (hashLiteralIndex < 0)
            {
                return false;
            }

            var blankedText = string.Concat(
                rawText.AsSpan(0, hashLiteralIndex),
                "\"integrityHash\":\"\"",
                rawText.AsSpan(hashLiteralIndex + hashLiteral.Length));

            var computedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(blankedText));
            return CryptographicOperations.FixedTimeEquals(computedHashBytes, suppliedHashBytes);
        }
    }
}
