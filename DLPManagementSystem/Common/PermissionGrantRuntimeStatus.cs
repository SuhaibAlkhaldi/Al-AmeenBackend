using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DLPManagementSystem.Models;

namespace DLPManagementSystem.Common
{
    public static class PermissionGrantRuntimeStatus
    {
        // Single source of truth for the Active/Pending/Expired/Revoked classification, expressed
        // as a plain expression tree over raw scalar values. Compute() below compiles it once for
        // ordinary C# use; MatchesStatus() substitutes its parameters with real PermissionGrant
        // property accesses to build a SQL-translatable predicate for EF Core queries. Both paths
        // read this exact same expression, so they cannot silently drift apart.
        public static readonly Expression<Func<DateTimeOffset?, DateTimeOffset?, DateTimeOffset, DateTimeOffset, string>> Classify =
            (revokedAtUtc, expiresAtUtc, startsAtUtc, nowUtc) =>
                revokedAtUtc != null ? "Revoked"
                : (expiresAtUtc != null && expiresAtUtc <= nowUtc) ? "Expired"
                : (startsAtUtc > nowUtc) ? "Pending"
                : "Active";

        private static readonly Func<DateTimeOffset?, DateTimeOffset?, DateTimeOffset, DateTimeOffset, string> Compiled = Classify.Compile();

        public static string Compute(DateTimeOffset? revokedAtUtc, DateTimeOffset? expiresAtUtc, DateTimeOffset startsAtUtc, DateTimeOffset nowUtc)
        {
            return Compiled(revokedAtUtc, expiresAtUtc, startsAtUtc, nowUtc);
        }

        // "This grant's runtime status equals `status`", built from Classify so it translates to
        // SQL — lets a status filter run as part of the query (CountAsync/Skip/Take all at the SQL
        // level) instead of requiring the full result set to be materialized first.
        public static Expression<Func<PermissionGrant, bool>> MatchesStatus(string status, DateTimeOffset nowUtc)
        {
            var grant = Expression.Parameter(typeof(PermissionGrant), "g");

            var substitutions = new Dictionary<ParameterExpression, Expression>
            {
                [Classify.Parameters[0]] = Expression.Property(grant, nameof(PermissionGrant.RevokedAtUtc)),
                [Classify.Parameters[1]] = Expression.Property(grant, nameof(PermissionGrant.ExpiresAtUtc)),
                [Classify.Parameters[2]] = Expression.Property(grant, nameof(PermissionGrant.StartsAtUtc)),
                [Classify.Parameters[3]] = Expression.Constant(nowUtc, typeof(DateTimeOffset))
            };

            var body = new ParameterSubstitutionVisitor(substitutions).Visit(Classify.Body)!;
            var equalsStatus = Expression.Equal(body, Expression.Constant(status, typeof(string)));

            return Expression.Lambda<Func<PermissionGrant, bool>>(equalsStatus, grant);
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
