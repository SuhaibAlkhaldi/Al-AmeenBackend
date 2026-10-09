using DLPManagementSystem.Helper.FileTracking;

namespace DLPManagementSystem.Tests.Service
{
    public class LineDiffTests
    {
        [Fact]
        public void IdenticalTexts_AreAllEqual()
        {
            var changes = LineDiff.Compare(["a", "b"], ["a", "b"]);

            Assert.NotNull(changes);
            Assert.All(changes, change => Assert.Equal(LineChangeKind.Equal, change.Kind));
            Assert.Equal(2, changes.Count);
        }

        [Fact]
        public void AppendedLine_IsAdded()
        {
            var changes = LineDiff.Compare(["a", "b"], ["a", "b", "c"]);

            Assert.NotNull(changes);
            Assert.Equal(new LineChange(LineChangeKind.Added, "c"), changes.Last());
            Assert.Single(changes, change => change.Kind == LineChangeKind.Added);
            Assert.DoesNotContain(changes, change => change.Kind == LineChangeKind.Removed);
        }

        [Fact]
        public void RemovedMiddleLine_IsRemoved()
        {
            var changes = LineDiff.Compare(["a", "b", "c"], ["a", "c"]);

            Assert.NotNull(changes);
            Assert.Equal(new LineChange(LineChangeKind.Removed, "b"), changes.Single(change => change.Kind != LineChangeKind.Equal));
        }

        [Fact]
        public void ChangedLine_IsRemovedAndAdded()
        {
            var changes = LineDiff.Compare(["a", "b", "c"], ["a", "B", "c"]);

            Assert.NotNull(changes);
            Assert.Single(changes, change => change.Kind == LineChangeKind.Removed && change.Text == "b");
            Assert.Single(changes, change => change.Kind == LineChangeKind.Added && change.Text == "B");
        }

        [Fact]
        public void EmptyOldText_IsAllAdded()
        {
            var changes = LineDiff.Compare([], ["x", "y"]);

            Assert.NotNull(changes);
            Assert.Equal([LineChangeKind.Added, LineChangeKind.Added], changes.Select(change => change.Kind));
        }

        [Fact]
        public void BothEmpty_HasNoLines()
        {
            var changes = LineDiff.Compare([], []);

            Assert.NotNull(changes);
            Assert.Empty(changes);
        }

        [Fact]
        public void TooManyChangedLines_ReturnsNull()
        {
            // 2100 lines fully replaced means 4200 edits, above MaxEditDistance.
            var oldLines = Enumerable.Range(0, 2100).Select(i => "a" + i).ToList();
            var newLines = Enumerable.Range(0, 2100).Select(i => "b" + i).ToList();

            Assert.Null(LineDiff.Compare(oldLines, newLines));
        }

        [Fact]
        public void LargeDocument_WithSmallEdit_ComparesQuickly()
        {
            var oldLines = Enumerable.Range(1, 50_000).Select(i => "line" + i).ToList();
            var newLines = new List<string>(oldLines);
            newLines[24_999] = "replaced";
            newLines.Add("appended");

            var changes = LineDiff.Compare(oldLines, newLines);

            Assert.NotNull(changes);
            Assert.Single(changes, change => change.Kind == LineChangeKind.Removed && change.Text == "line25000");
            Assert.Single(changes, change => change.Kind == LineChangeKind.Added && change.Text == "replaced");
            Assert.Single(changes, change => change.Kind == LineChangeKind.Added && change.Text == "appended");
        }

        [Fact]
        public void Output_RebuildsBothTexts_OnRandomEdits()
        {
            var random = new Random(7);

            for (var round = 0; round < 200; round++)
            {
                var oldLines = Enumerable.Range(0, random.Next(0, 60)).Select(_ => "L" + random.Next(0, 12)).ToList();
                var newLines = MutateLines(oldLines, random);

                var changes = LineDiff.Compare(oldLines, newLines);

                Assert.NotNull(changes);
                Assert.Equal(newLines, changes.Where(change => change.Kind != LineChangeKind.Removed).Select(change => change.Text));
                Assert.Equal(oldLines, changes.Where(change => change.Kind != LineChangeKind.Added).Select(change => change.Text));
            }
        }

        private static List<string> MutateLines(List<string> source, Random random)
        {
            var result = new List<string>(source);
            var edits = random.Next(0, 8);
            for (var edit = 0; edit < edits; edit++)
            {
                var action = random.Next(3);
                if (action == 0 || result.Count == 0)
                {
                    result.Insert(random.Next(0, result.Count + 1), "L" + random.Next(0, 12));
                }
                else if (action == 1)
                {
                    result.RemoveAt(random.Next(result.Count));
                }
                else
                {
                    result[random.Next(result.Count)] = "L" + random.Next(0, 12);
                }
            }
            return result;
        }
    }
}
