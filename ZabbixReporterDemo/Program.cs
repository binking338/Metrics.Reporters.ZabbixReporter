using Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZabbixReporterDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Metric.Config
                .WithHttpEndpoint("http://localhost:1234/")
                .WithErrorHandler(x => Console.WriteLine(x.ToString()))
                .WithInternalMetrics()
                .WithReporting(config => config
                    .WithConsoleReport(TimeSpan.FromSeconds(30))
                    .WithZabbix("MetricsDemo", TimeSpan.FromSeconds(5))
                );
            Console.ReadLine();
        }
    }
}
