# OpenGauss - the .NET data provider for PostgreSQL

[![stable](https://img.shields.io/nuget/v/OpenGauss.svg?label=stable)](https://www.nuget.org/packages/OpenGauss/)
[![next patch](https://img.shields.io/myget/opengauss/v/opengauss.svg?label=next%20patch)](https://www.myget.org/feed/opengauss/package/nuget/OpenGauss)
[![daily builds (vnext)](https://img.shields.io/myget/opengauss-unstable/v/opengauss.svg?label=unstable)](https://www.myget.org/feed/opengauss-unstable/package/nuget/OpenGauss)
[![build](https://img.shields.io/github/workflow/status/opengauss/opengauss/Build)](https://github.com/opengauss/opengauss/actions)
[![gitter](https://img.shields.io/badge/gitter-join%20chat-brightgreen.svg)](https://gitter.im/opengauss/opengauss)

## What is OpenGauss?

OpenGauss is the open source .NET data provider for PostgreSQL. It allows you to connect and interact with PostgreSQL server using .NET.

For the full documentation, please visit [the OpenGauss website](https://www.opengauss.org). For the Entity Framework Core provider that works with this provider, see [OpenGauss.EntityFrameworkCore.PostgreSQL](https://github.com/opengauss/efcore.pg).

## Quickstart

Here's a basic code snippet to get you started:

```csharp
var connString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

await using var conn = new OpenGaussConnection(connString);
await conn.OpenAsync();

// Insert some data
await using (var cmd = new OpenGaussCommand("INSERT INTO data (some_field) VALUES (@p)", conn))
{
    cmd.Parameters.AddWithValue("p", "Hello world");
    await cmd.ExecuteNonQueryAsync();
}

// Retrieve all rows
await using (var cmd = new OpenGaussCommand("SELECT some_field FROM data", conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
}
```

## Key features

* High-performance PostgreSQL driver. Regularly figures in the top contenders on the [TechEmpower Web Framework Benchmarks](https://www.techempower.com/benchmarks/).
* Full support of most PostgreSQL types, including advanced ones such as arrays, enums, ranges, multiranges, composites, JSON, PostGIS and others.
* Highly-efficient bulk import/export API.
* Failover, load balancing and general multi-host support.
* Great integration with Entity Framework Core via [OpenGauss.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/OpenGauss.EntityFrameworkCore.PostgreSQL).

For the full documentation, please visit the OpenGauss website at [https://www.opengauss.org](https://www.opengauss.org).



## TEST

### BUGTEST

Bug1645 -> OpenGauss.PostgresException : 42P07: relation "data" already exists
Bug2278 -> OpenGauss.PostgresException : 0A000: DOMAIN is not yet supported.
Bug2296 -> OpenGauss.PostgresException : 42P07: relation "data" already exists
Bug3649 -> OpenGauss.PostgresException : 42601: syntax error at or near "binary"

Chunked_char_array_write_buffer_encoding_space -> OpenGauss.PostgresException : 42601: syntax error at or near "BINARY"
Chunked_string_write_buffer_encoding_space -> OpenGauss.PostgresException : 42601: syntax error at or near "BINARY"

###  ConnectionTests

Connect_OptionsFromEnvironment_Succeeds -> SetEnvironmentVariable("PGOPTIONS", "-c default_transaction_isolation=serializable -c default_transaction_deferrable=on -c foo.bar=My"))

### LargeObjectTests 

OpenGauss.PostgresException : 0A000: openGauss does not support large object yet

### TypeMapperTests

String_to_citext -> OpenGauss.PostgresException : 58P01: could not open extension control file: No such file or directory


StatementOID_legacy_batching -> CREATE TABLE ... WITH OIDS is not yet supported.


### SchemaTests

Column_schema_data_types -> type line is not yet supported

MetaDataCollections -> type pg_node_tree is not yet supported.

Precision_and_scale -> type pg_node_tree is not yet supported

### NodaTimeTests

Interval_as_Duration_with_months_fails -> function make_interval(months := integer) does not exist