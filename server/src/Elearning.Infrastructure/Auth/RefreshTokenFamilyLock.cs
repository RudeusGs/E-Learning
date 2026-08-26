using System.Buffers.Binary;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Auth;

internal static class RefreshTokenFamilyLock
{
    public static Task<int> AcquireAsync(
        ElearningDbContext dbContext,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var lockId = GetLockId(familyId);
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockId});",
            cancellationToken);
    }

    private static long GetLockId(Guid familyId)
    {
        Span<byte> bytes = stackalloc byte[16];
        familyId.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes[..8]) ^
            BinaryPrimitives.ReadInt64LittleEndian(bytes[8..]);
    }
}
