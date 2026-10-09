namespace DLPManagementSystem.Service.Service
{
    // One time a piece of content was observed on a device, attributed to the employee holding that device.
    public sealed record PropagationObservation(DateTimeOffset ObservedAtUtc, Guid EmployeeId, Guid DeviceId);

    // A continuous stretch of time during which some sliding window held at least the threshold number of
    // distinct employees. Members are every observation inside [StartUtc, EndUtc].
    public sealed record PropagationCluster(
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        IReadOnlyList<PropagationObservation> Members)
    {
        public int DistinctEmployeeCount => Members.Select(m => m.EmployeeId).Distinct().Count();

        public int DistinctDeviceCount => Members.Select(m => m.DeviceId).Distinct().Count();
    }

    // Pure detection logic for the "Rapid File Propagation" rule (File Tracking plan, Phase 5). Kept free of
    // any database access so the rule itself can be verified exhaustively in isolation.
    //
    // Rule: for one content fingerprint, a window of length `window` containing at least `employeeThreshold`
    // DISTINCT employees qualifies. Distinct means distinct EmployeeId - the same person on two devices counts
    // once. Detection uses the time each observation was recorded (not any transfer time) and makes no claim
    // about how the content moved.
    //
    // Algorithm: time-ordered two-pointer sliding window with a per-employee frequency map. Each right-hand
    // position shrinks the left edge until the span is within the window, then counts distinct employees.
    // Consecutive qualifying positions are merged into one cluster while the gap between them stays within
    // the window, so a continuous burst produces one cluster (and therefore one alert), while a burst separated
    // by a quiet gap longer than the window produces another.
    public static class RapidPropagationDetector
    {
        public static IReadOnlyList<PropagationCluster> FindClusters(
            IEnumerable<PropagationObservation> observations,
            int employeeThreshold,
            TimeSpan window)
        {
            if (employeeThreshold < 1) throw new ArgumentOutOfRangeException(nameof(employeeThreshold));
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));

            var sorted = observations.OrderBy(o => o.ObservedAtUtc).ThenBy(o => o.EmployeeId).ToList();
            if (sorted.Count < employeeThreshold) return [];

            var frequency = new Dictionary<Guid, int>();
            var clusters = new List<PropagationCluster>();

            DateTimeOffset? clusterStart = null;
            DateTimeOffset? clusterEnd = null;

            var left = 0;
            for (var right = 0; right < sorted.Count; right++)
            {
                Increment(frequency, sorted[right].EmployeeId);

                while (sorted[right].ObservedAtUtc - sorted[left].ObservedAtUtc > window)
                {
                    Decrement(frequency, sorted[left].EmployeeId);
                    left++;
                }

                if (frequency.Count < employeeThreshold) continue;

                var windowStart = sorted[left].ObservedAtUtc;
                var windowEnd = sorted[right].ObservedAtUtc;

                if (clusterStart is null)
                {
                    clusterStart = windowStart;
                    clusterEnd = windowEnd;
                }
                else if (windowEnd - clusterEnd!.Value <= window)
                {
                    clusterEnd = windowEnd;
                }
                else
                {
                    clusters.Add(BuildCluster(sorted, clusterStart.Value, clusterEnd!.Value));
                    clusterStart = windowStart;
                    clusterEnd = windowEnd;
                }
            }

            if (clusterStart is not null)
            {
                clusters.Add(BuildCluster(sorted, clusterStart.Value, clusterEnd!.Value));
            }

            return clusters;
        }

        private static PropagationCluster BuildCluster(
            IReadOnlyList<PropagationObservation> sorted,
            DateTimeOffset startUtc,
            DateTimeOffset endUtc)
        {
            var members = sorted
                .Where(o => o.ObservedAtUtc >= startUtc && o.ObservedAtUtc <= endUtc)
                .ToList();
            return new PropagationCluster(startUtc, endUtc, members);
        }

        private static void Increment(Dictionary<Guid, int> frequency, Guid employeeId) =>
            frequency[employeeId] = frequency.GetValueOrDefault(employeeId) + 1;

        private static void Decrement(Dictionary<Guid, int> frequency, Guid employeeId)
        {
            var remaining = frequency[employeeId] - 1;
            if (remaining == 0) frequency.Remove(employeeId);
            else frequency[employeeId] = remaining;
        }
    }
}
