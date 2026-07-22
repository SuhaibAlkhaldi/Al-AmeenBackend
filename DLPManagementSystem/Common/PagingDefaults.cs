using System;

namespace DLPManagementSystem.Common
{
    // Shared page-size bound for every list endpoint, so a client can't request an unbounded page.
    // Clamps rather than errors, so existing calls (e.g. pageSize=20) keep working unchanged.
    public static class PagingDefaults
    {
        public const int MinPageSize = 1;
        public const int MaxPageSize = 100;

        public static int ClampPageSize(int pageSize)
        {
            return Math.Clamp(pageSize, MinPageSize, MaxPageSize);
        }
    }
}
