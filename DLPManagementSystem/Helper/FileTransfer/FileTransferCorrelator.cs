namespace DLPManagementSystem.Helper.FileTransfer
{
    // An inventory record a browser observation may refer to. Only the fields matching needs are carried, so the rule
    // stays a pure function over plain values.
    public sealed record CorrelationCandidate(
        Guid FileId,
        string? FileHash,
        string? ContentFingerprint,
        string FileName,
        long SizeBytes,
        string ClassificationTier,
        DateTimeOffset LastScanAtUtc);

    // The browser's view of the file at send time. ContentFingerprint is normally null here, because the browser cannot
    // compute it for DOCX/PDF; the agent supplies it in a later step.
    public sealed record FileObservation(
        string FileName,
        long SizeBytes,
        string? FileHashBytes,
        string? ContentFingerprint,
        DateTimeOffset ObservedAtUtc);

    public sealed record CorrelationResult(
        string FileMatch,
        Guid? FileId,
        string? ContentFingerprint,
        string? ClassificationTier);

    // Matches one observation against the candidates from the SAME device, in strict order. The first rule that matches
    // wins. Matching across devices is never done by byte hash (each device stamps its own watermark, so the bytes differ),
    // which is why the device is part of the query that fills the candidate list.
    public static class FileTransferCorrelator
    {
        // Name/size/time matching only trusts a candidate scanned close to the send. A wider window would let an unrelated
        // file with the same name and size look like the one that was sent.
        public static readonly TimeSpan NameSizeTimeWindow = TimeSpan.FromMinutes(30);

        public static CorrelationResult Correlate(FileObservation observation, IReadOnlyList<CorrelationCandidate> candidates)
        {
            if (!string.IsNullOrEmpty(observation.FileHashBytes))
            {
                var byHash = candidates
                    .Where(c => string.Equals(c.FileHash, observation.FileHashBytes, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.LastScanAtUtc)
                    .FirstOrDefault();

                if (byHash is not null)
                {
                    return new CorrelationResult(FileTransferEvidenceRules.FileMatch_ByteHash, byHash.FileId, byHash.ContentFingerprint, byHash.ClassificationTier);
                }
            }

            if (!string.IsNullOrEmpty(observation.ContentFingerprint))
            {
                var byFingerprint = candidates
                    .Where(c => string.Equals(c.ContentFingerprint, observation.ContentFingerprint, StringComparison.Ordinal))
                    .OrderByDescending(c => c.LastScanAtUtc)
                    .FirstOrDefault();

                if (byFingerprint is not null)
                {
                    return new CorrelationResult(FileTransferEvidenceRules.FileMatch_Fingerprint, byFingerprint.FileId, byFingerprint.ContentFingerprint, byFingerprint.ClassificationTier);
                }
            }

            var nameSizeTime = candidates
                .Where(c => string.Equals(c.FileName, observation.FileName, StringComparison.OrdinalIgnoreCase)
                    && c.SizeBytes == observation.SizeBytes
                    && (observation.ObservedAtUtc - c.LastScanAtUtc).Duration() <= NameSizeTimeWindow)
                .ToList();

            if (nameSizeTime.Count == 1)
            {
                var only = nameSizeTime[0];
                return new CorrelationResult(FileTransferEvidenceRules.FileMatch_NameSizeTimeUnique, only.FileId, only.ContentFingerprint, only.ClassificationTier);
            }

            if (nameSizeTime.Count > 1)
            {
                // The file is unknown, so the event's tier is the most sensitive candidate. Keeping the most sensitive one is
                // the conservative choice: a Public-only set is still rejected, and a mixed set is kept.
                var strongest = nameSizeTime
                    .OrderByDescending(c => FileTransferEvidenceRules.TierRank(c.ClassificationTier))
                    .First();

                return new CorrelationResult(FileTransferEvidenceRules.FileMatch_NameSizeTimeAmbiguous, null, null, strongest.ClassificationTier);
            }

            return new CorrelationResult(FileTransferEvidenceRules.FileMatch_NoMatch, null, null, null);
        }
    }
}
