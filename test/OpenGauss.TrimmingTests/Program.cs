using System;
using OpenGauss;

var connectionString = Environment.GetEnvironmentVariable("NPGSQL_TEST_DB")
                       ?? "Server=localhost;Username=opengauss_tests;Password=opengauss_tests;Database=opengauss_tests;Timeout=0;Command Timeout=0";

await using var conn = new OpenGaussConnection(connectionString);
await conn.OpenAsync();
await using var cmd = new OpenGaussCommand("SELECT 'Hello World'", conn);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var value = reader.GetFieldValue<string>(0);
    if (value != "Hello World")
        throw new Exception($"Got {value} instead of the expected 'Hello World'");
}
