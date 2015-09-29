using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZabbixApi.Entities;
using ZabbixApi.Services;

namespace Metrics.Reporters
{
    public class ZabbixConfig
    {
        protected string _url = null, _user = null, _password = null;

        protected Dictionary<string, Item> _ItemsCache = new Dictionary<string, Item>();
        protected Host _HostCache = null;
        protected HostGroup _HostGroupMetricsCache = null;
        protected Template _TemplateCache = null;
        protected Application _ApplicationCache = null;

        protected Func<ZabbixApi.Context> _contextCreator = null;

        public ZabbixConfig(string server, string user, string password)
        {
            ZabbixApi.Helper.Check.NotNull(server, "server");

            _url = System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.url"];
            if (string.IsNullOrWhiteSpace(_url)) _url = string.Format("http://{0}/zabbix/api_jsonrpc.php", server);
            _user = user ?? System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.user"];
            _password = password ?? System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.password"];

            _contextCreator = (Func<ZabbixApi.Context>)(() => { return new ZabbixApi.Context(_url, _user, _password); });
            if (!TryCreateHost(GetLocalHostname()))
            {
                throw new Exception(string.Format("Zabbix server have not configured host named \"{0}\""));
            }
        }

        public bool TryCreateTrapperItem(string key, string units = "", Item.ValueType valueType = Item.ValueType.NumericUnsigned, Item.DataType dataType = Item.DataType.Decimal)
        {
            if (_ItemsCache.ContainsKey(key)) return true;
            using (var context = _contextCreator())
            {
                try
                {
                    var service = new ItemService(context);
                    var items = service.Get(new { key_ = key, hostid = _TemplateCache.Id });
                    var item = items == null || items.Count() == 0 ? null : items.First();
                    if (item == null)
                    {
                        item = new Item();
                        item.name = key;
                        item.key_ = key;
                        item.type = Item.ItemType.ZabbixTrapper;
                        item.value_type = valueType;
                        if (valueType == Item.ValueType.NumericUnsigned) item.data_type = dataType;
                        item.units = units;
                        item.hostid = _TemplateCache.Id;
                        var addition = new Dictionary<string, object>();
                        addition["applications"] = new string[] { _ApplicationCache.Id };
                        item.Id = service.Create(item, addition);
                    }
                    if (item.Id != null)
                    {
                        _ItemsCache[key] = item;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MetricsErrorHandler.Handle(ex, string.Format("Error on configuring zabbix trapper item, zabbix api {0}", _url));
                    return false;
                }
            }
        }

        public bool TryCreateTemplate(string name)
        {
            if (!TryCreateHostGroup("Metrics.NET")) return false;

            using (var context = _contextCreator())
            {
                try
                {
                    var service = new ZabbixApi.Services.TemplateService(context);
                    var template = service.GetByPropety("name", name);
                    if (template == null)
                    {
                        template = new Template();
                        template.host = name;
                        template.name = name;
                        template.groups = new List<HostGroup> { _HostGroupMetricsCache };
                        template.hosts = new List<Host> { _HostCache };
                        template.Id = service.Create(template);
                    }
                    else
                    {
                        if (template.hosts.FirstOrDefault(h => h.Id == _HostCache.Id) == null)
                        {
                            template.hosts.Add(_HostCache);
                            if (service.Update(template) == null) return false;
                        }
                    }
                    if (template.Id != null)
                    {
                        TryCreateApplication(name, template.Id);
                        _TemplateCache = template;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MetricsErrorHandler.Handle(ex, string.Format("Error on configuring zabbix template, zabbix api {0}", _url));
                    return false;
                }
            }
        }

        public bool TryCreateHostGroup(string name)
        {
            using (var context = _contextCreator())
            {
                try
                {
                    var service = new ZabbixApi.Services.HostGroupService(context);
                    var hostGroup = service.GetByName(name);
                    if (hostGroup == null)
                    {
                        hostGroup = new HostGroup();
                        hostGroup.name = name;
                        hostGroup.Id = service.Create(hostGroup);

                    }
                    if (hostGroup.Id != null)
                    {
                        _HostGroupMetricsCache = hostGroup;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MetricsErrorHandler.Handle(ex, string.Format("Error on configuring zabbix host group, zabbix api {0}", _url));
                    return false;
                }
            }
        }

        public bool TryCreateApplication(string name, string hostid)
        {
            using (var context = _contextCreator())
            {
                try
                {
                    var service = new ZabbixApi.Services.ApplicationService(context);
                    var applications = service.Get(new { name = name, hostid = hostid });
                    var application = applications == null || applications.Count() == 0 ? null : applications.First();
                    if (application == null)
                    {
                        application = new Application();
                        application.hostid = hostid;
                        application.name = name;
                        application.Id = service.Create(application);
                    }
                    if (application.Id != null)
                    {
                        _ApplicationCache = application;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MetricsErrorHandler.Handle(ex, string.Format("Error on configuring zabbix application, zabbix api {0}", _url));
                    return false;
                }
            };
        }

        public bool TryCreateHost(string name)
        {
            ZabbixApi.Helper.Check.IsNotNullOrWhiteSpace(name, "name");
            if (!TryCreateHostGroup("Metrics.NET")) return false;

            using (var context = _contextCreator())
            {
                try
                {
                    var service = new ZabbixApi.Services.HostService(context);
                    var host = service.GetByName(name);
                    if (host == null)
                    {
                        if (IsIpAddress(name))
                        {
                            host = new Host();
                            host.host = name;
                            host.interfaces = host.interfaces ?? new List<HostInterface>();
                            host.interfaces.Add(new HostInterface()
                            {
                                type = HostInterface.InterfaceType.Agent,
                                main = true,
                                useip = true,
                                ip = name,
                                dns = "",
                                port = "10050"
                            });
                            host.groups = host.groups ?? new List<HostGroup>();
                            host.groups.Add(_HostGroupMetricsCache);
                            host.Id = service.Create(host);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (host.Id != null)
                    {
                        _HostCache = host;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MetricsErrorHandler.Handle(ex, string.Format("Error on create zabbix host, zabbix api {0}", _url));
                    return false;
                }
            }
        }

        /// <summary>
        /// System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.localhost"] or System.Net.Dns.GetHostName()
        /// </summary>
        /// <returns></returns>
        public static string GetLocalHostname()
        {
            var localhost = System.Configuration.ConfigurationManager.AppSettings["ZabbixApi.localhost"];
            return string.IsNullOrWhiteSpace(localhost) ? System.Net.Dns.GetHostName() : localhost;
        }

        private static bool IsIpAddress(string ip)
        {
            System.Net.IPAddress add;
            return System.Net.IPAddress.TryParse(ip, out add);

        }
    }
}
