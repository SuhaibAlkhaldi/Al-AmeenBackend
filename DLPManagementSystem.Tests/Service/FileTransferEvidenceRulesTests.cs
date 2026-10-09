using DLPManagementSystem.Helper.FileTransfer;

namespace DLPManagementSystem.Tests.Service
{
    public class FileTransferEvidenceRulesTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
        private static readonly Guid FileA = Guid.Parse("00000000-0000-0000-0000-0000000000f1");

        [Theory]
        [InlineData("HIGH", "HIGH", "HIGH")]
        [InlineData("HIGH", "MEDIUM", "MEDIUM")]
        [InlineData("MEDIUM", "HIGH", "MEDIUM")]
        [InlineData("MEDIUM", "MEDIUM", "MEDIUM")]
        [InlineData("LOW", "HIGH", "LOW")]
        [InlineData("HIGH", "LOW", "LOW")]
        [InlineData("MEDIUM", "LOW", "LOW")]
        [InlineData("LOW", "LOW", "LOW")]
        public void Overall_IsTheWeakerOfFileAndRecipient_WithLowRecipientForcingLow(string file, string recipient, string expected)
        {
            Assert.Equal(expected, FileTransferEvidenceRules.Overall(file, recipient));
        }

        [Theory]
        [InlineData("NONE", "HIGH")]
        [InlineData("NONE", "LOW")]
        [InlineData("HIGH", "NONE")]
        public void Overall_NoneOnEitherSide_MeansNoEvent(string file, string recipient)
        {
            Assert.Equal("NONE", FileTransferEvidenceRules.Overall(file, recipient));
        }

        [Theory]
        [InlineData("BYTE_HASH_MATCH", "HIGH")]
        [InlineData("FINGERPRINT_MATCH", "MEDIUM")]
        [InlineData("NAME_SIZE_TIME_UNIQUE", "MEDIUM")]
        [InlineData("NAME_SIZE_TIME_AMBIGUOUS", "LOW")]
        [InlineData("NO_MATCH", "NONE")]
        [InlineData("SOMETHING_ELSE", "NONE")]
        public void FileLevel_MapsEachMatchType(string match, string expected)
        {
            Assert.Equal(expected, FileTransferEvidenceRules.FileLevel(match));
        }

        [Theory]
        [InlineData("EMAIL_IN_SEND_FORM", "HIGH")]
        [InlineData("EMAIL_PARTIAL_READ", "MEDIUM")]
        [InlineData("TEAMS_CHAT_NAME", "MEDIUM")]
        [InlineData("CHAT_CONTACT_NAME", "LOW")]
        [InlineData("GROUP_NAME", "LOW")]
        [InlineData("UNKNOWN_KIND", "NONE")]
        public void RecipientLevel_MapsEachEvidenceType(string evidence, string expected)
        {
            Assert.Equal(expected, FileTransferEvidenceRules.RecipientLevel(evidence));
        }

        [Theory]
        [InlineData("Public", true)]
        [InlineData("Internal", true)]
        [InlineData("Secret", true)]
        [InlineData("Very_Secret", true)]
        [InlineData("", false)]
        [InlineData("Unknown", false)]
        [InlineData(null, false)]
        public void KnownTiers_ProduceEvents_UnknownDoesNot(string? tier, bool expected)
        {
            Assert.Equal(expected, FileTransferEvidenceRules.IsKnownTier(tier));
        }

        [Fact]
        public void Correlate_SameHash_IsByteHashMatch()
        {
            var candidates = new[] { Candidate(FileA, hash: "abc", name: "[Secret] plan.pdf", size: 10, tier: "Secret") };
            var observation = new FileObservation("[Secret] plan.pdf", 10, "abc", null, Now);

            var result = FileTransferCorrelator.Correlate(observation, candidates);

            Assert.Equal("BYTE_HASH_MATCH", result.FileMatch);
            Assert.Equal(FileA, result.FileId);
            Assert.Equal("Secret", result.ClassificationTier);
        }

        [Fact]
        public void Correlate_NoHashButUniqueNameSizeNearTime_IsNameSizeTimeUnique()
        {
            var candidates = new[] { Candidate(FileA, hash: "abc", name: "[Secret] plan.pdf", size: 10, tier: "Secret") };
            var observation = new FileObservation("[Secret] plan.pdf", 10, null, null, Now);

            Assert.Equal("NAME_SIZE_TIME_UNIQUE", FileTransferCorrelator.Correlate(observation, candidates).FileMatch);
        }

        [Fact]
        public void Correlate_NameSizeOutsideWindow_IsNoMatch()
        {
            var candidates = new[] { Candidate(FileA, hash: "abc", name: "[Secret] plan.pdf", size: 10, tier: "Secret", scannedMinutesBefore: 120) };
            var observation = new FileObservation("[Secret] plan.pdf", 10, null, null, Now);

            Assert.Equal("NO_MATCH", FileTransferCorrelator.Correlate(observation, candidates).FileMatch);
        }

        [Fact]
        public void Correlate_TwoNameSizeCandidates_IsAmbiguousAndUsesStrongestTier()
        {
            var candidates = new[]
            {
                Candidate(Guid.NewGuid(), hash: "a", name: "report.pdf", size: 5, tier: "Internal"),
                Candidate(Guid.NewGuid(), hash: "b", name: "report.pdf", size: 5, tier: "Very_Secret")
            };
            var observation = new FileObservation("report.pdf", 5, null, null, Now);

            var result = FileTransferCorrelator.Correlate(observation, candidates);

            Assert.Equal("NAME_SIZE_TIME_AMBIGUOUS", result.FileMatch);
            Assert.Null(result.FileId);
            Assert.Equal("Very_Secret", result.ClassificationTier);
        }

        [Fact]
        public void Correlate_HashMatchBeatsNameSizeMatch()
        {
            var byHash = Guid.NewGuid();
            var byName = Guid.NewGuid();
            var candidates = new[]
            {
                Candidate(byName, hash: "other", name: "plan.pdf", size: 10, tier: "Secret"),
                Candidate(byHash, hash: "same", name: "renamed.pdf", size: 99, tier: "Secret")
            };
            var observation = new FileObservation("plan.pdf", 10, "same", null, Now);

            Assert.Equal(byHash, FileTransferCorrelator.Correlate(observation, candidates).FileId);
        }

        [Fact]
        public void Correlate_NoCandidates_IsNoMatch()
        {
            var result = FileTransferCorrelator.Correlate(new FileObservation("x.pdf", 1, "h", null, Now), Array.Empty<CorrelationCandidate>());

            Assert.Equal("NO_MATCH", result.FileMatch);
            Assert.Null(result.FileId);
        }

        private static CorrelationCandidate Candidate(Guid fileId, string hash, string name, long size, string tier, int scannedMinutesBefore = 1) =>
            new(fileId, hash, null, name, size, tier, Now.AddMinutes(-scannedMinutesBefore));
    }
}
