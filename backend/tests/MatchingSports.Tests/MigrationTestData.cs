using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Tests;

internal static class MigrationTestData
{
    // Seed the historical schema with SQL: the current EF model includes later audit columns.
    public static Task InsertUserAsync(MatchingSportsDbContext db, User user) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (id, email, normalized_email, user_name, normalized_user_name,
                email_confirmed, access_failed_count, two_factor_enabled, lockout_enabled)
            VALUES ({user.Id}, {user.Email}, {user.NormalizedEmail}, {user.UserName}, {user.NormalizedUserName},
                {user.EmailConfirmed}, 0, FALSE, FALSE)
            """);
}
