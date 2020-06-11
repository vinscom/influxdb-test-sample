using System;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using Task = System.Threading.Tasks.Task;

namespace Examples
{
    public static class QueriesWritesExample
    {
        public static async Task Main(string[] args)
        {
            InfluxDBClientOptions options = InfluxDBClientOptions
                .Builder
                .CreateNew()
                .Bucket("bucket_name")
                .Url("http://192.168.0.10:8086")
                .Org("nothing")
                .Build();

            var influxDBClient = InfluxDBClientFactory.Create(options);
            
            //
            // Write Data
            //
            using (var writeApi = influxDBClient.GetWriteApi())
            {
                //
                // Write by Point
                //
                var point = PointData.Measurement("temperature")
                    .Tag("location", "west")
                    .Field("value", 55D)
                    .Timestamp(DateTime.UtcNow.AddSeconds(-10), WritePrecision.Ns);

                writeApi.WritePoint("bucket_name", "org_id", point);

                //
                // Write by LineProtocol
                //
                writeApi.WriteRecord("bucket_name", "org_id", WritePrecision.Ns, "temperature,location=north value=60.0");

                //
                // Write by POCO
                //
                var temperature = new Temperature { Location = "south", Value = 62D, Time = DateTime.UtcNow };
                writeApi.WriteMeasurement("bucket_name", "org_id", WritePrecision.Ns, temperature);
            }

            //
            // Query data
            //
            var flux = "from(bucket:\"bucket_name\") |> range(start: 0)";

            Query d = new Query(query: flux, db: "bucket_name");

            var fluxTables = await influxDBClient.GetQueryApi().QueryAsync(d,"org_id");
            fluxTables.ForEach(fluxTable =>
            {
                var fluxRecords = fluxTable.Records;
                fluxRecords.ForEach(fluxRecord =>
                {
                    Console.WriteLine($"{fluxRecord.GetTime()}: {fluxRecord.GetValue()}");
                });
            });

            influxDBClient.Dispose();
        }

        [Measurement("temperature")]
        private class Temperature
        {
            [Column("location", IsTag = true)] public string Location { get; set; }

            [Column("value")] public double Value { get; set; }

            [Column(IsTimestamp = true)] public DateTime Time;
        }
    }
}