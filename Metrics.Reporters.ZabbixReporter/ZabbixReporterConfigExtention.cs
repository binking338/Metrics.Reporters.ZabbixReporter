using Metrics.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metrics
{
    public static class ZabbixReporterConfigExtention
    {
        /// <summary>
        /// 数据导出至Zabbix服务器
        /// Zabbix服务配置读取至ZabbixApi的配置文件
        /// </summary>
        /// <param name="reports"></param>
        /// <param name="timeInterval">上报时间间隔</param>
        /// <returns></returns>
        public static MetricsReports WithZabbix(this MetricsReports reports, TimeSpan timeInterval)
        {
            return reports.WithZabbix(null, 0, null, null, timeInterval);
        }


        /// <summary>
        /// 数据导出至Zabbix服务器
        /// </summary>
        /// <param name="reports"></param>
        /// <param name="template">Zabbix模板</param>
        /// <param name="timeInterval">上报时间间隔</param>
        /// <returns></returns>
        public static MetricsReports WithZabbix(this MetricsReports reports, string template, TimeSpan timeInterval)
        {
            return reports.WithZabbix(null, 0, null, null, template, timeInterval);
        }

        /// <summary>
        /// 数据导出至Zabbix服务器
        /// </summary>
        /// <param name="reports"></param>
        /// <param name="host">Zabbix服务IP地址</param>
        /// <param name="port">Zabbix服务端口</param>
        /// <param name="user">Zabbix管理端用户账户</param>
        /// <param name="password">Zabbix管理端用户密码</param>
        /// <param name="timeInterval">上报时间间隔</param>
        /// <returns></returns>
        public static MetricsReports WithZabbix(this MetricsReports reports, string host, int port, string user, string password, TimeSpan timeInterval)
        {
            return reports.WithReport(new Metrics.Reporters.ZabbixReporter(host, port, user, password), timeInterval);
        }
        /// <summary>
        /// 数据导出至Zabbix服务器
        /// </summary>
        /// <param name="reports"></param>
        /// <param name="host">Zabbix服务IP地址</param>
        /// <param name="port">Zabbix服务端口</param>
        /// <param name="user">Zabbix管理端用户账户</param>
        /// <param name="password">Zabbix管理端用户密码</param>
        /// <param name="template">Zabbix模板</param>
        /// <param name="timeInterval">上报时间间隔</param>
        /// <returns></returns>
        public static MetricsReports WithZabbix(this MetricsReports reports, string host, int port, string user, string password, string template, TimeSpan timeInterval)
        {
            host = host ?? GetZabbixHost();
            if (port <= 0) port = GetZabbixPort();
            return reports.WithReport(new Metrics.Reporters.ZabbixReporter(host, port, user, password, template), timeInterval);
        }

        private static string GetZabbixHost()
        {
            var host = System.Configuration.ConfigurationManager.AppSettings["Zabbix.host"];
            if (!string.IsNullOrWhiteSpace(host)) return host;
            var url = System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.url"];
            if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Invalid ZabbixApi Configuration: ZabbixApi.url must be non empty string");
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) throw new InvalidOperationException("Invalid ZabbixApi Configuration: ZabbixApi.url must be absolute url");
            return uri.Host;
        }

        private static int GetZabbixPort()
        {
            int port;
            var strPort = System.Configuration.ConfigurationManager.AppSettings["Zabbix.port"];
            int.TryParse(strPort, out port);
            return port > 0 ? port : 10051;
        }
    }
}
