using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Metrics.Utils;

namespace Metrics.Reporters
{
    public class ZabbixReporter : BaseReport
    {
        protected Queue<ItemValue> _sendQueue = new Queue<ItemValue>();

        protected string Hostname { get; set; }
        protected ZabbixSender ZabbixSender { get; set; }
        protected ZabbixConfig ZabbixConfig { get; set; }

        public ZabbixReporter(string zabbixServer, int port = 10051, string user = null, string password = null, string template = null)
        {
            template = template ?? GetGlobalContextName();
            Hostname = ZabbixConfig.GetLocalHostname();
            ZabbixSender = new ZabbixSender(zabbixServer, port);
            try
            {
                ZabbixConfig = new ZabbixConfig(zabbixServer, user, password);
                ZabbixConfig.TryCreateTemplate(template);
            }
            catch(Exception ex)
            {
                MetricsErrorHandler.Handle(ex);
            }
        }

        protected override void StartReport(string contextName)
        {
            _sendQueue.Clear();
            base.StartReport(contextName);
        }

        protected override void EndReport(string contextName)
        {
            base.EndReport(contextName);
            ZabbixSender.Send(_sendQueue);
            _sendQueue.Clear();
        }

        protected override void ReportGauge(string name, double value, Unit unit, MetricTags tags)
        {
            ItemValue item = null;
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                if (null != ZabbixConfig) ZabbixConfig.TryCreateTrapperItem(name, unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                item = NewItemValue(name, value);
                _sendQueue.Enqueue(item);
            }
        }

        protected override void ReportCounter(string name, MetricData.CounterValue value, Unit unit, MetricTags tags)
        {
            ItemValue item = null;
            if (null != ZabbixConfig) ZabbixConfig.TryCreateTrapperItem(name, unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
            item = NewItemValue(name, value.Count);
            _sendQueue.Enqueue(item);
            if (value.Items != null && value.Items.Length != 0)
            {
                foreach (var itm in value.Items)
                {
                    if (null != ZabbixConfig)
                    {
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, itm.Item), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderNameAsPercent(name, itm.Item), Unit.Percent.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                    }
                    item = NewItemValue(SubfolderName(name, itm.Item), itm.Count);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderNameAsPercent(name, itm.Item), itm.Percent);
                    _sendQueue.Enqueue(item);
                }
            }
        }

        protected override void ReportHistogram(string name, MetricData.HistogramValue value, Unit unit, MetricTags tags)
        {
            ReportHistogram(name, value, unit, tags, true);
        }

        protected void ReportHistogram(string name, MetricData.HistogramValue value, Unit unit, MetricTags tags, bool withMainCount)
        {
            ItemValue item = null;
            if (null != ZabbixConfig)
            {
                if (withMainCount) ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Count"), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Last"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Min"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Mean"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Median"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Max"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "p75"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "p95"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "p98"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "p99"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "p999"), unit.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "StdDev"), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Sample"), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
            }
            if (withMainCount)
            {
                item = NewItemValue(SubfolderName(name, "Count"), value.Count);
                _sendQueue.Enqueue(item);
            }
            item = NewItemValue(SubfolderName(name, "Last"), value.LastValue);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Min"), value.Min);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Mean"), value.Mean);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Median"), value.Median);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Max"), value.Max);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "p75"), value.Percentile75);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "p95"), value.Percentile95);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "p98"), value.Percentile98);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "p99"), value.Percentile99);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "p999"), value.Percentile999);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "StdDev"), value.StdDev);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Sample"), value.SampleSize);
            _sendQueue.Enqueue(item);
        }

        protected override void ReportMeter(string name, MetricData.MeterValue value, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            ReportMeter(name, value, unit, rateUnit, tags, true);
        }
        protected void ReportMeter(string name, MetricData.MeterValue value, Unit unit, TimeUnit rateUnit, MetricTags tags, bool withMainCount)
        {
            ItemValue item = null;
            if (null != ZabbixConfig)
            {
                if (withMainCount) ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Count"), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Rate-Mean"), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Rate-1-min"), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Rate-5-min"), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "Rate-15-min"), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
            }
            if (withMainCount)
            {
                item = NewItemValue(SubfolderName(name, "Count"), value.Count);
                _sendQueue.Enqueue(item);
            }
            item = NewItemValue(SubfolderName(name, "Rate-Mean"), value.MeanRate);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Rate-1-min"), value.OneMinuteRate);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Rate-5-min"), value.FiveMinuteRate);
            _sendQueue.Enqueue(item);
            item = NewItemValue(SubfolderName(name, "Rate-15-min"), value.FifteenMinuteRate);
            _sendQueue.Enqueue(item);
            if (value.Items != null && value.Items.Length != 0)
            {
                foreach (var itm in value.Items)
                {
                    if (null != ZabbixConfig)
                    {
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, SubfolderName(itm.Item, "Count")), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderNameAsPercent(name, itm.Item), Unit.Percent.ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, SubfolderName(itm.Item, "Rate-Mean")), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, SubfolderName(itm.Item, "Rate-1-min")), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, SubfolderName(itm.Item, "Rate-5-min")), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                        ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, SubfolderName(itm.Item, "Rate-15-min")), rateUnit.Unit().ToString(), ZabbixApi.Entities.Item.ValueType.NumericFloat);
                    }
                    item = NewItemValue(SubfolderName(name, SubfolderName(itm.Item, "Count")), itm.Value.Count);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderNameAsPercent(name, itm.Item), itm.Percent);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderName(name, SubfolderName(itm.Item, "Rate-Mean")), itm.Value.MeanRate);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderName(name, SubfolderName(itm.Item, "Rate-1-min")), itm.Value.OneMinuteRate);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderName(name, SubfolderName(itm.Item, "Rate-5-min")), itm.Value.FiveMinuteRate);
                    _sendQueue.Enqueue(item);
                    item = NewItemValue(SubfolderName(name, SubfolderName(itm.Item, "Rate-15-min")), itm.Value.FifteenMinuteRate);
                    _sendQueue.Enqueue(item);
                }
            }
        }

        protected override void ReportTimer(string name, MetricData.TimerValue value, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            ItemValue item = null;
            if (null != ZabbixConfig)
            {
                ZabbixConfig.TryCreateTrapperItem(SubfolderName(name, "ActiveSessions"), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned);
            }
            item = NewItemValue(SubfolderName(name, "ActiveSessions"), value.ActiveSessions);
            _sendQueue.Enqueue(item);
            ReportMeter(name, value.Rate, unit, rateUnit, tags);
            ReportHistogram(SubfolderName(name, "Duration"), value.Histogram, Unit.Custom(durationUnit.Unit()), tags, false);
        }

        protected override void ReportHealth(HealthStatus status)
        {
            ItemValue item = null;
            foreach (var itm in status.Results)
            {
                if (null != ZabbixConfig)
                {
                    ZabbixConfig.TryCreateTrapperItem(SubfolderName(typeof(HealthStatus).Name, itm.Name), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.NumericUnsigned, ZabbixApi.Entities.Item.DataType.Boolean);
                    ZabbixConfig.TryCreateTrapperItem(SubfolderName(typeof(HealthStatus).Name, SubfolderName(itm.Name, "Message")), Unit.None.ToString(), ZabbixApi.Entities.Item.ValueType.Text);
                }

                item = NewItemValue(SubfolderName(typeof(HealthStatus).Name, itm.Name), itm.Check.IsHealthy);
                _sendQueue.Enqueue(item);
                if (!itm.Check.IsHealthy)
                {
                    // 仅在健康检查不通过的时候上报具体信息
                    item = NewItemValue(SubfolderName(typeof(HealthStatus).Name, SubfolderName(itm.Name, "Message")), itm.Check.Message);
                    _sendQueue.Enqueue(item);
                }
            }
        }

        protected virtual ItemValue NewItemValue(string key, object value)
        {
            return new ItemValue() { Key = key, Value = value, Host = Hostname };
        }

        protected virtual string SubfolderName(string name, string subname)
        {
            return string.Concat(name, ".", subname);
        }
        protected virtual string SubfolderNameAsPercent(string name, string subname)
        {
            return string.Concat(name, ".", subname, "-per-");
        }

        protected override string FormatContextName(IEnumerable<string> contextStack, string contextName)
        {
            string name = "";

            if (contextStack.Count() > 0)
            {
                name = string.Join("-", contextStack.Skip(1).Concat(new string[] { contextName }));
            }
            name = Regex.Replace(name, @"[^a-zA-Z0-9\-_\.]", "");
            return name;
        }

        protected override string FormatMetricName<T>(string context, Metrics.MetricData.MetricValueSource<T> metric)
        {
            string name = string.Concat(context, "-", metric.Name);
            name = Regex.Replace(name, @"[^a-zA-Z0-9\-_\.]", "");
            return name;
        }

        protected virtual string GetGlobalContextName()
        {
            try
            {
                var configName = System.Configuration.ConfigurationManager.AppSettings["Metrics.GlobalContextName"];
                var name = string.IsNullOrEmpty(configName) ? System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace('.', '_') : configName;
                return name;
            }
            catch (Exception x)
            {
                throw new InvalidOperationException("Invalid Metrics Configuration: Metrics.GlobalContextName must be non empty string", x);
            }
        }
    }
}
