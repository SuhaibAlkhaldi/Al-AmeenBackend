using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DLPManagementSystem.Models;

namespace DLPManagementSystem.Common
{
    // Mirrors PermissionGrantRuntimeStatus's shape exactly (see that file for the full rationale):
    // Classify is the single source of truth, expressed as a plain expression tree over raw scalar
    // values so Compute() (ordinary C#) and MatchesStatus() (SQL-translatable EF predicate) can
    // never drift apart. The Active/Exhausted/Expired/Revoked classification itself matches the
    // frontend's existing EnrollmentTokensListComponent.status() exactly: Revoked takes precedence
    // over Expired, which takes precedence over Exhausted.
    public static class AgentEnrollmentTokenRuntimeStatus
    {
        public static readonly Expression<Func<DateTimeOffset?, DateTimeOffset, int, int, DateTimeOffset, string>> Classify =
            (revokedAtUtc, expiresAtUtc, usedCount, maxUses, nowUtc) =>
                revokedAtUtc != null ? "Revoked"
                : expiresAtUtc <= nowUtc ? "Expired"
                : usedCount >= maxUses ? "Exhausted"
                : "Active";

        private static readonly Func<DateTimeOffset?, DateTimeOffset, int, int, DateTimeOffset, string> Compiled = Classify.Compile();

        public static string Compute(DateTimeOffset? revokedAtUtc, DateTimeOffset expiresAtUtc, int usedCount, int maxUses, DateTimeOffset nowUtc)
        {
            return Compiled(revokedAtUtc, expiresAtUtc, usedCount, maxUses, nowUtc);
        }

        // "This token's runtime status equals `status`", built from Classify so it translates to
        // SQL — lets a status filter run as part of the query (CountAsync/Skip/Take all at the SQL
        // level) instead of requiring the full result set to be materialized first.
        public static Expression<Func<AgentEnrollmentToken, bool>> MatchesStatus(string status, DateTimeOffset nowUtc)
        {
            var token = Expression.Parameter(typeof(AgentEnrollmentToken), "t");

            var substitutions = new Dictionary<ParameterExpression, Expression>
            {
                [Classify.Parameters[0]] = Expression.Property(token, nameof(AgentEnrollmentToken.RevokedAtUtc)),
                [Classify.Parameters[1]] = Expression.Property(token, nameof(AgentEnrollmentToken.ExpiresAtUtc)),
                [Classify.Parameters[2]] = Expression.Property(token, nameof(AgentEnrollmentToken.UsedCount)),
                [Classify.Parameters[3]] = Expression.Property(token, nameof(AgentEnrollmentToken.MaxUses)),
                [Classify.Parameters[4]] = Expression.Constant(nowUtc, typeof(DateTimeOffset))
            };

            var body = new ParameterSubstitutionVisitor(substitutions).Visit(Classify.Body)!;
            var equalsStatus = Expression.Equal(body, Expression.Constant(status, typeof(string)));

            return Expression.Lambda<Func<AgentEnrollmentToken, bool>>(equalsStatus, token);
        }

        private sealed class ParameterSubstitutionVisitor : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, Expression> _map;

            public ParameterSubstitutionVisitor(Dictionary<ParameterExpression, Expression> map)
            {
                _map = map;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return _map.TryGetValue(node, out var replacement) ? replacement : base.VisitParameter(node);
            }
        }
    }
}
