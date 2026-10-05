using System.Text.RegularExpressions;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Helper.FileTracking
{
    // One file's lineage is every FileId that ever sat at the same device + folder + base name (the name with its
    // classification tag stripped), because a tag change renames the file and a rename always gets a fresh FileId.
    // The File History timeline and the diff viewer both use this one definition, so they always agree on which
    // versions belong to the same file.
    public static class FileLineage
    {
        // Mirrors FilenameClassificationTagger's tag pattern on the agent (CompanyDlp.Core): the classification tag
        // the agent prepends to a file name is not part of the file's identity.
        private static readonly Regex ClassificationTagPattern = new(
            @"^\[(Public|Internal|Restricted|Secret|Very[ _]Secret)\]\s+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static async Task<List<(Guid FileId, DateTimeOffset FirstSeenAtUtc)>> ResolveAsync(
            DLPSystemContext db,
            Guid organizationId,
            FileInventoryRecord record,
            CancellationToken cancellationToken = default)
        {
            var baseName = StripClassificationTag(record.FileName);
            var folder = GetFolder(record.FilePath);

            var candidates = await db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.DeviceId == record.DeviceId && x.FileName.Contains(baseName))
                .Select(x => new { x.FileId, x.FileName, x.FilePath, x.FirstSeenAtUtc })
                .ToListAsync(cancellationToken);

            return candidates
                .Where(x => string.Equals(StripClassificationTag(x.FileName), baseName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(GetFolder(x.FilePath), folder, StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.FileId, x.FirstSeenAtUtc))
                .ToList();
        }

        public static string StripClassificationTag(string fileName) =>
            ClassificationTagPattern.Replace(fileName, string.Empty, 1);

        public static string GetFolder(string filePath)
        {
            var index = filePath.LastIndexOf('\\');
            return index < 0 ? string.Empty : filePath[..index];
        }
    }
}
