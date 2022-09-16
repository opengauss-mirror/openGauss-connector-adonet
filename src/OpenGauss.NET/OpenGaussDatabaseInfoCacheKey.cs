using System;

namespace OpenGauss.NET
{
    readonly struct OpenGaussDatabaseInfoCacheKey : IEquatable<OpenGaussDatabaseInfoCacheKey>
    {
        public readonly int Port;
        public readonly string? Host;
        public readonly string? Database;
        public readonly ServerCompatibilityMode CompatibilityMode;

        public OpenGaussDatabaseInfoCacheKey(OpenGaussConnectionStringBuilder connectionString)
        {
            Port = connectionString.Port;
            Host = connectionString.Host;
            Database = connectionString.Database;
            CompatibilityMode = connectionString.ServerCompatibilityMode;
        }

        public bool Equals(OpenGaussDatabaseInfoCacheKey other) =>
            Port == other.Port &&
            Host == other.Host &&
            Database == other.Database &&
            CompatibilityMode == other.CompatibilityMode;

        public override bool Equals(object? obj) =>
            obj is OpenGaussDatabaseInfoCacheKey key && key.Equals(this);

        public override int GetHashCode() =>
            HashCode.Combine(Port, Host, Database, CompatibilityMode);
    }
}
