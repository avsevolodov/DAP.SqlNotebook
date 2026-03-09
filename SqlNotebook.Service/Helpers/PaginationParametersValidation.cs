using System;

namespace DAP.SqlNotebook.Service.Helpers;

public static class PaginationParametersValidation
{
    public static void Validate(int offset, int batchSize)
    {
        if (batchSize is <= 0 or > 1000)
        {
            throw new ArgumentException("BatchSize must be between 0 and 1000.");
        }

        if (offset < 0)
        {
            throw new ArgumentException($"Offset must be between 0 and {int.MaxValue}.");
        }
    }
}