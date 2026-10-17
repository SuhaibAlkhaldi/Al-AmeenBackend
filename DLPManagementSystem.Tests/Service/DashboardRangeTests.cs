using DLPManagementSystem.CompanyDlpDashboard;

namespace DLPManagementSystem.Tests.Service
{
    public class DashboardRangeTests
    {
        private static readonly DateTimeOffset NowUtc = new(2036, 1, 15, 10, 23, 47, TimeSpan.Zero);

        [Theory]
        [InlineData("6h", DashboardRangeKind.SixHours, 6)]
        [InlineData("24h", DashboardRangeKind.TwentyFourHours, 24)]
        [InlineData("7d", DashboardRangeKind.SevenDays, 168)]
        public void TryParse_AcceptsEachValidRangeValue(string raw, DashboardRangeKind expectedKind, int expectedHours)
        {
            var ok = DashboardRange.TryParse(raw, NowUtc, out var range, out var error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(expectedKind, range.Kind);
            Assert.Equal(raw, range.Key);
            Assert.Equal(TimeSpan.FromHours(expectedHours), range.ToUtc - range.FromUtc);
        }

        [Fact]
        public void TryParse_IsCaseInsensitive()
        {
            var ok = DashboardRange.TryParse("7D", NowUtc, out var range, out _);

            Assert.True(ok);
            Assert.Equal(DashboardRangeKind.SevenDays, range.Kind);
        }

        [Fact]
        public void TryParse_NullOrEmpty_DefaultsTo24h()
        {
            var ok = DashboardRange.TryParse(null, NowUtc, out var range, out _);

            Assert.True(ok);
            Assert.Equal(DashboardRangeKind.TwentyFourHours, range.Kind);
            Assert.Equal("24h", range.Key);
        }

        [Fact]
        public void TryParse_EmptyOrWhitespace_DefaultsTo24hLikeOmittingIt()
        {
            Assert.True(DashboardRange.TryParse("", NowUtc, out var rangeForEmpty, out _));
            Assert.Equal("24h", rangeForEmpty.Key);

            Assert.True(DashboardRange.TryParse("   ", NowUtc, out var rangeForWhitespace, out _));
            Assert.Equal("24h", rangeForWhitespace.Key);
        }

        [Fact]
        public void TryParse_TrimsAndIgnoresCase_ButStillRequiresAValidToken()
        {
            // Surrounding whitespace and case are tolerated (trimmed/lower-invariant'd) - "24H " is
            // still a valid 24h, not a 400.
            Assert.True(DashboardRange.TryParse("24H ", NowUtc, out var range, out _));
            Assert.Equal(DashboardRangeKind.TwentyFourHours, range.Kind);
        }

        [Theory]
        [InlineData("1h")]
        [InlineData("week")]
        [InlineData("7days")]
        public void TryParse_RejectsAnythingElse(string raw)
        {
            var ok = DashboardRange.TryParse(raw, NowUtc, out var range, out var error);

            Assert.False(ok);
            Assert.Null(range);
            Assert.NotNull(error);
        }

        [Fact]
        public void TryParse_FloorsToUtcToTheStartOfTheCurrentMinute()
        {
            DashboardRange.TryParse("24h", NowUtc, out var range, out _);

            Assert.Equal(new DateTimeOffset(2036, 1, 15, 10, 23, 0, TimeSpan.Zero), range.ToUtc);
        }

        [Fact]
        public void TryParse_FromUtc_IsToUtcMinusTheDuration()
        {
            DashboardRange.TryParse("6h", NowUtc, out var range, out _);

            Assert.Equal(range.ToUtc - TimeSpan.FromHours(6), range.FromUtc);
        }
    }
}
