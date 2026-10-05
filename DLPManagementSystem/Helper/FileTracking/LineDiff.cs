namespace DLPManagementSystem.Helper.FileTracking
{
    public enum LineChangeKind
    {
        Equal,
        Added,
        Removed
    }

    public sealed record LineChange(LineChangeKind Kind, string Text);

    // Line-based comparison of two versions of a file's text for the File Tracking diff viewer (Phase 6). Uses
    // Myers' O(ND) algorithm, the one git uses: its cost follows the number of changed lines rather than the
    // file's size, so a long document with a small edit compares quickly. Pure - no I/O, no database - so the
    // caller decides which texts to compare and how much of the result to show.
    public static class LineDiff
    {
        // Above this many changed lines the comparison is refused rather than attempted. Myers keeps one snapshot
        // per edit step, so memory grows with the square of this value: 2000 steps is about 16 MB at most.
        public const int MaxEditDistance = 2000;

        // Returns null when the texts differ by more than MaxEditDistance lines (the caller reports the diff as
        // unavailable rather than showing a partial result). Otherwise returns every line in order, each tagged
        // as unchanged, added (only in the new text) or removed (only in the old text).
        public static IReadOnlyList<LineChange>? Compare(IReadOnlyList<string> oldLines, IReadOnlyList<string> newLines)
        {
            var n = oldLines.Count;
            var m = newLines.Count;
            var max = n + m;
            var offset = max + 1;

            // furthestX[offset + k] = the furthest old-line index reached on diagonal k (old index minus new index).
            var furthestX = new int[2 * max + 3];

            // snapshots[d] = the furthestX values on the diagonals in [-d-1, d+1], taken before step d runs. Backtracking
            // needs them to recover the path, which is why they are kept rather than rebuilt.
            var snapshots = new List<int[]>();

            var limit = Math.Min(max, MaxEditDistance);
            for (var d = 0; d <= limit; d++)
            {
                snapshots.Add(SnapshotAt(furthestX, offset, d));

                for (var k = -d; k <= d; k += 2)
                {
                    // Step down (an added line) or right (a removed line), whichever reaches further.
                    var x = k == -d || (k != d && furthestX[offset + k - 1] < furthestX[offset + k + 1])
                        ? furthestX[offset + k + 1]
                        : furthestX[offset + k - 1] + 1;
                    var y = x - k;

                    while (x < n && y < m && string.Equals(oldLines[x], newLines[y], StringComparison.Ordinal))
                    {
                        x++;
                        y++;
                    }

                    furthestX[offset + k] = x;

                    if (x >= n && y >= m)
                    {
                        return Backtrack(snapshots, oldLines, newLines, d);
                    }
                }
            }

            return null;
        }

        private static int[] SnapshotAt(int[] furthestX, int offset, int d)
        {
            var snapshot = new int[2 * d + 3];
            for (var index = 0; index < snapshot.Length; index++)
            {
                var k = index - (d + 1);
                snapshot[index] = furthestX[offset + k];
            }
            return snapshot;
        }

        private static List<LineChange> Backtrack(List<int[]> snapshots, IReadOnlyList<string> oldLines, IReadOnlyList<string> newLines, int finalD)
        {
            var changes = new List<LineChange>();
            var x = oldLines.Count;
            var y = newLines.Count;

            for (var d = finalD; d >= 0; d--)
            {
                var snapshot = snapshots[d];
                var k = x - y;

                // Which diagonal this step came from: the one that reached further, same rule as the forward pass.
                var previousK = k == -d || (k != d && snapshot[SnapshotIndex(k - 1, d)] < snapshot[SnapshotIndex(k + 1, d)])
                    ? k + 1
                    : k - 1;
                var previousX = snapshot[SnapshotIndex(previousK, d)];
                var previousY = previousX - previousK;

                // The diagonal run between the previous point and this step's start is unchanged lines.
                while (x > previousX && y > previousY)
                {
                    changes.Add(new LineChange(LineChangeKind.Equal, oldLines[x - 1]));
                    x--;
                    y--;
                }

                if (d > 0)
                {
                    changes.Add(x == previousX
                        ? new LineChange(LineChangeKind.Added, newLines[previousY])
                        : new LineChange(LineChangeKind.Removed, oldLines[previousX]));
                }

                x = previousX;
                y = previousY;
            }

            changes.Reverse();
            return changes;
        }

        private static int SnapshotIndex(int k, int d) => k + d + 1;
    }
}
