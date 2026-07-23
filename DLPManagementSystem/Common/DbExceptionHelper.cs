using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Common
{
    /// <summary>
    /// Helpers for recognizing specific SQL Server error conditions wrapped inside EF Core's
    /// <see cref="DbUpdateException"/>, so callers can catch a precise condition (e.g. a unique-constraint
    /// violation) instead of swallowing every possible <see cref="DbUpdateException"/> indiscriminately.
    /// </summary>
    public static class DbExceptionHelper
    {
        /// <summary>
        /// True when the exception's inner <see cref="SqlException"/> is a unique-constraint/unique-index
        /// violation (SQL Server error 2601 or 2627) — e.g. the race-condition window between an
        /// application-level duplicate check and the actual insert, where two concurrent requests both
        /// pass the check before either commits.
        /// </summary>
        public static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}
