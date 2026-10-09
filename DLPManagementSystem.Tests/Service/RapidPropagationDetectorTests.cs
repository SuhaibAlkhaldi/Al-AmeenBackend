using DLPManagementSystem.Service.Service;

namespace DLPManagementSystem.Tests.Service
{
    public class RapidPropagationDetectorTests
    {
        private static readonly DateTimeOffset Day = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

        private static readonly Guid EmployeeA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        private static readonly Guid EmployeeB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        private static readonly Guid EmployeeC = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        private static readonly Guid EmployeeD = Guid.Parse("00000000-0000-0000-0000-00000000000d");

        private static readonly Guid DeviceOne = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid DeviceTwo = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid DeviceThree = Guid.Parse("00000000-0000-0000-0000-000000000003");

        private static PropagationObservation At(int minute, Guid employee, Guid device) =>
            new(Day.AddMinutes(minute), employee, device);

        private static readonly TimeSpan ThirtyMinutes = TimeSpan.FromMinutes(30);

        [Fact]
        public void ThreeDistinctEmployeesWithinTheWindow_ProducesOneCluster()
        {
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(7, EmployeeB, DeviceTwo),
                At(12, EmployeeC, DeviceThree),
            };

            var clusters = RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes);

            var cluster = Assert.Single(clusters);
            Assert.Equal(3, cluster.DistinctEmployeeCount);
            Assert.Equal(Day.AddMinutes(0), cluster.StartUtc);
            Assert.Equal(Day.AddMinutes(12), cluster.EndUtc);
        }

        [Fact]
        public void TheSamePersonOnTwoDevicesCountsOnce()
        {
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(5, EmployeeA, DeviceTwo),
                At(10, EmployeeB, DeviceThree),
            };

            Assert.Empty(RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes));
        }

        [Fact]
        public void CounterExample_FindsTheQualifyingWindowThatAGreedyAnchorWouldMiss()
        {
            // 09:00 A, 09:29 B, 09:31 C, 09:58 D with a 30 minute window and threshold 3:
            // the qualifying window is [09:29 -> 09:58] (B, C, D). A greedy anchor split into pairs misses it.
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(29, EmployeeB, DeviceTwo),
                At(31, EmployeeC, DeviceThree),
                At(58, EmployeeD, DeviceOne),
            };

            var cluster = Assert.Single(RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes));
            Assert.Equal(Day.AddMinutes(29), cluster.StartUtc);
            Assert.Equal(Day.AddMinutes(58), cluster.EndUtc);
            Assert.Equal(3, cluster.DistinctEmployeeCount);
        }

        [Fact]
        public void ContinuousBurst_ProducesOneClusterNotOnePerNewMember()
        {
            // Four employees over 25 minutes: the first qualifying window is reached at 09:20, and the fourth
            // employee joining at 09:25 extends the same burst instead of raising a second alert.
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(10, EmployeeB, DeviceTwo),
                At(20, EmployeeC, DeviceThree),
                At(25, EmployeeD, DeviceOne),
            };

            var clusters = RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes);

            var cluster = Assert.Single(clusters);
            Assert.Equal(4, cluster.DistinctEmployeeCount);
            Assert.Equal(Day.AddMinutes(0), cluster.StartUtc);
            Assert.Equal(Day.AddMinutes(25), cluster.EndUtc);
        }

        [Fact]
        public void BurstsSeparatedByAQuietGapLongerThanTheWindow_ProduceSeparateClusters()
        {
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(5, EmployeeB, DeviceTwo),
                At(10, EmployeeC, DeviceThree),
                At(200, EmployeeA, DeviceTwo),
                At(205, EmployeeB, DeviceOne),
                At(210, EmployeeD, DeviceThree),
            };

            var clusters = RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes);

            Assert.Equal(2, clusters.Count);
            Assert.Equal(Day.AddMinutes(0), clusters[0].StartUtc);
            Assert.Equal(Day.AddMinutes(200), clusters[1].StartUtc);
        }

        [Fact]
        public void SpreadBeyondTheWindow_DoesNotQualify()
        {
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(31, EmployeeB, DeviceTwo),
                At(62, EmployeeC, DeviceThree),
            };

            Assert.Empty(RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes));
        }

        [Fact]
        public void FewerThanTheThreshold_ProducesNothing()
        {
            var observations = new[]
            {
                At(0, EmployeeA, DeviceOne),
                At(5, EmployeeB, DeviceTwo),
            };

            Assert.Empty(RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes));
        }

        [Fact]
        public void ObservationsArrivingOutOfOrder_AreEvaluatedInTimeOrder()
        {
            var observations = new[]
            {
                At(12, EmployeeC, DeviceThree),
                At(0, EmployeeA, DeviceOne),
                At(7, EmployeeB, DeviceTwo),
            };

            var cluster = Assert.Single(RapidPropagationDetector.FindClusters(observations, 3, ThirtyMinutes));
            Assert.Equal(Day.AddMinutes(0), cluster.StartUtc);
        }
    }
}
