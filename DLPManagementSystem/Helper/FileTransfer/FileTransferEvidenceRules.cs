namespace DLPManagementSystem.Helper.FileTransfer
{
    // Evidence rules for FileTransferEvent (Phase 7, see the frozen evidence spec). Pure functions only: no database,
    // no clock, so every rule can be tested directly. Bump RuleVersion whenever a mapping or the matching order changes,
    // so each stored event records which rules produced its levels.
    public static class FileTransferEvidenceRules
    {
        public const int RuleVersion = 1;

        public const string Recipient_Email = "EMAIL";
        public const string Recipient_ChatName = "CHAT_NAME";
        public const string Recipient_GroupName = "GROUP_NAME";
        public const string Recipient_Unknown = "UNKNOWN";

        public const string Level_High = "HIGH";
        public const string Level_Medium = "MEDIUM";
        public const string Level_Low = "LOW";
        public const string Level_None = "NONE";

        public const string FileMatch_ByteHash = "BYTE_HASH_MATCH";
        public const string FileMatch_Fingerprint = "FINGERPRINT_MATCH";
        public const string FileMatch_NameSizeTimeUnique = "NAME_SIZE_TIME_UNIQUE";
        public const string FileMatch_NameSizeTimeAmbiguous = "NAME_SIZE_TIME_AMBIGUOUS";
        public const string FileMatch_NoMatch = "NO_MATCH";

        public const string RecipientEvidence_EmailInSendForm = "EMAIL_IN_SEND_FORM";
        public const string RecipientEvidence_EmailPartialRead = "EMAIL_PARTIAL_READ";
        public const string RecipientEvidence_TeamsChatName = "TEAMS_CHAT_NAME";
        public const string RecipientEvidence_ChatContactName = "CHAT_CONTACT_NAME";
        public const string RecipientEvidence_GroupName = "GROUP_NAME";

        // Higher rank = stronger. Used for the "weakest of the two" rule.
        private static int Rank(string level) => level switch
        {
            Level_High => 3,
            Level_Medium => 2,
            Level_Low => 1,
            _ => 0
        };

        public static string FileLevel(string fileMatch) => fileMatch switch
        {
            FileMatch_ByteHash => Level_High,
            FileMatch_Fingerprint => Level_Medium,
            FileMatch_NameSizeTimeUnique => Level_Medium,
            FileMatch_NameSizeTimeAmbiguous => Level_Low,
            _ => Level_None
        };

        public static string RecipientLevel(string recipientEvidence) => recipientEvidence switch
        {
            RecipientEvidence_EmailInSendForm => Level_High,
            RecipientEvidence_EmailPartialRead => Level_Medium,
            RecipientEvidence_TeamsChatName => Level_Medium,
            RecipientEvidence_ChatContactName => Level_Low,
            RecipientEvidence_GroupName => Level_Low,
            _ => Level_None
        };

        // NONE file evidence means no event is recorded at all. A LOW recipient forces LOW overall. Otherwise the
        // overall level is the weaker of the file and recipient levels. No points are added and no percentages are used.
        public static string Overall(string fileLevel, string recipientLevel)
        {
            if (Rank(fileLevel) == 0 || Rank(recipientLevel) == 0) return Level_None;
            if (recipientLevel == Level_Low) return Level_Low;

            return Rank(fileLevel) <= Rank(recipientLevel) ? fileLevel : recipientLevel;
        }

        // Every classification tier is recorded, Public included (decision 2026-10-06). A file with no known tier is not
        // recorded, because the event would have no classification to show.
        public static bool IsKnownTier(string? tier) =>
            tier is "Public" or "Internal" or "Secret" or "Very_Secret";

        // Stronger tiers win when several candidates match an ambiguous name/size pair.
        public static int TierRank(string tier) => tier switch
        {
            "Very_Secret" => 3,
            "Secret" => 2,
            "Internal" => 1,
            _ => 0
        };
    }
}
