using System;

namespace DataAccess.Shared {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TableInfoAttribute(string? tableName = null, string? primaryKeyName = null, string? sequenceName = null, bool isIdentity = false) : Attribute {
        public string? TableName { get; } = tableName;
        public string? PrimaryKeyName { get; } = primaryKeyName;
        public string? SequenceName { get; } = sequenceName;
        public bool IsIdentity { get; } = isIdentity;
    }
}