using System.Data;
using System.Data.Common;
using SqlArtisan.Dapper;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// Runs a <c>DateTimePart</c> cell of SQLA0104's datepart table member by member,
/// so each per-engine twin checks its whole list rather than one rejected value.
/// </summary>
internal static class DatepartCells
{
    public static TheoryData<DateTimePart> EveryMember
    {
        get
        {
            TheoryData<DateTimePart> data = new();
            foreach (DateTimePart part in Enum.GetValues<DateTimePart>())
            {
                data.Add(part);
            }

            return data;
        }
    }

    public static bool Runs(IDbConnection connection, ISqlBuilder query)
    {
        try
        {
            connection.ExecuteScalar(query);
            return true;
        }
        catch (DbException)
        {
            return false;
        }
    }
}
