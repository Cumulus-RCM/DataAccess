using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.Shared;

public record SaveResult(string TableName, int UpdatedCount = 0, int DeletedCount = 0, IEnumerable<IdPk>? InsertedIds = null, Exception? Exception = null) {
    public SaveResult(string tableName, IdPk insertedId) : this(new SaveResult(tableName,InsertedIds: new[] { insertedId })){}
}

public record SaveResponse(IReadOnlyCollection<SaveResult> SaveResults) : Response(SaveResults.FirstOrDefault(x => x.Exception is not null)?.Exception) {
    public override string ToString() {
        if (SaveResults.Count == 0) return "No Updates.";
        var sb = new StringBuilder();
        foreach (var saveResult in SaveResults) {
            sb.Append($"Table:{saveResult.TableName} Updated:{saveResult.UpdatedCount} Deleted:{saveResult.DeletedCount} Inserted:{saveResult.InsertedIds?.Count()}");
            if (saveResult.Exception is not null) sb.Append($"Exception:{saveResult.Exception.Message}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static SaveResponse Failed(string message) => new([]) {Exception = new Exception(message)};
    public static SaveResponse Empty() => new([]);
    
    public static SaveResponse Merge(IEnumerable<SaveResponse> saveResponses) {
        var saveResults = saveResponses
            .SelectMany(response => response.SaveResults)
            .GroupBy(x => x.TableName)
            .Select(x => new SaveResult(
                x.Key,
                x.Sum(y => y.UpdatedCount),
                x.Sum(y => y.DeletedCount),
                x.SelectMany(y => y.InsertedIds ?? Array.Empty<IdPk>()).ToArray(),
                x.FirstOrDefault(y => y.Exception is not null)?.Exception))
            .ToArray();
        return new SaveResponse(saveResults);
    }
}

public class SaveResponseBuilder {
    private readonly List<SaveResult> saveResults = new();

    public SaveResponseBuilder Add(SaveResult saveResult) {
        saveResults.Add(saveResult);
        return this;
    }

    public SaveResponseBuilder Add(string tableName, int updatedCount = 0, int deletedCount = 0, IEnumerable<IdPk>? insertedIds = null, Exception? exception = null) {
        var saveResult = new SaveResult(tableName, updatedCount, deletedCount, insertedIds, exception);
        return Add(saveResult);
    }

    public SaveResponse Build() {
        //aggregate saveResults by TableName using linq
        var result = saveResults.GroupBy(x => x.TableName)
            .Select(x => new SaveResult(x.Key,
                x.Sum(y => y.UpdatedCount),
                x.Sum(y => y.DeletedCount),
                x.SelectMany(y => y.InsertedIds ?? Array.Empty<IdPk>()).ToArray(),
                x.FirstOrDefault(y => y.Exception is not null)?.Exception))
            .ToList();
        return new SaveResponse(result);
    }
}