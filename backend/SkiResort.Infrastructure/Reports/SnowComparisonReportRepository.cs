using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace SkiResort.Infrastructure.Reports;

public sealed class SnowComparisonRow
{
    public Guid ResortId { get; init; }
    public string ResortName { get; init; } = string.Empty;
    public DateTimeOffset ObservedAt { get; init; }
    public decimal SnowDepthCm { get; init; }
    public decimal NewSnowCm { get; init; }
}

public interface ISnowComparisonReportRepository
{
    Task<IReadOnlyList<SnowComparisonRow>> GetSnowComparisonAsync(
        IReadOnlyCollection<Guid> resortIds,
        CancellationToken cancellationToken = default);
}

public sealed class SnowComparisonReportRepository : ISnowComparisonReportRepository
{
    private readonly string _connectionString;

    public SnowComparisonReportRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IReadOnlyList<SnowComparisonRow>> GetSnowComparisonAsync(
        IReadOnlyCollection<Guid> resortIds,
        CancellationToken cancellationToken = default)
    {
        if (resortIds == null || resortIds.Count == 0)
        {
            return Array.Empty<SnowComparisonRow>();
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var fromInclusive = DateTimeOffset.UtcNow.AddDays(-7);

        const string sql = @"
select
    r.""Id"" as ""ResortId"",
    r.""Name"" as ""ResortName"",
    sc.""ObservedAt"" as ""ObservedAt"",
    sc.""SnowDepthCm"" as ""SnowDepthCm"",
    sc.""NewSnowCm"" as ""NewSnowCm""
from ""SnowConditions"" sc
join ""Resorts"" r on sc.""ResortId"" = r.""Id""
where sc.""ObservedAt"" >= @FromInclusive
  and sc.""ResortId"" = any(@ResortIds)
order by r.""Name"", sc.""ObservedAt"";
";

        var command = new CommandDefinition(
            sql,
            new
            {
                ResortIds = resortIds.ToArray(),
                FromInclusive = fromInclusive
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<SnowComparisonRow>(command);
        return rows.ToList();
    }
}

