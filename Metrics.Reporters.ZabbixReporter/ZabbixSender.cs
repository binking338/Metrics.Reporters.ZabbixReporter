using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metrics.Reporters
{
    public class ZabbixSender
    {
        private int _port;
        private string _zabbixServer;
        private Encoding _encoding;

        public Encoding Encoding { get { return _encoding; } set { _encoding = value; } }

        public ZabbixSender(string zabbixServer, int port = 10051)
        {
            _zabbixServer = zabbixServer;
            _port = port;
            _encoding = Encoding.UTF8;
        }

        public SenderResponse Send(string host, string itemKey, string value, int timeout = 500)
        {
            var item = new ItemValue();
            item.Host = host;
            item.Key = itemKey;
            item.Value = value;
            return Send(item, timeout);
        }

        public SenderResponse Send(ItemValue value, int timeout = 500)
        {
            return Send(new ItemValue[] { value }, timeout);
        }

        public SenderResponse Send(IEnumerable<ItemValue> values, int timeout = 500)
        {
            var req = new SenderRequest();
            req.Request = "sender data";
            req.Data = values.Cast<object>().ToArray();
            var jsonReq = JsonConvert.SerializeObject(req);
            using (var tcpClient = new TcpClient(_zabbixServer, _port))
            using (var networkStream = tcpClient.GetStream())
            {
                var data = _encoding.GetBytes(jsonReq);
                networkStream.Write(data, 0, data.Length);
                networkStream.Flush();
                var counter = 0;
                while (!networkStream.DataAvailable)
                {
                    if (counter < timeout / 50)
                    {
                        counter++;
                        Thread.Sleep(50);
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }

                var resbytes = new Byte[1024];
                networkStream.Read(resbytes, 0, resbytes.Length);
                var s = _encoding.GetString(resbytes);
                var jsonRes = s.Substring(s.IndexOf('{'));
                return JsonConvert.DeserializeObject<SenderResponse>(jsonRes);
            }
        }
    }

    public class SenderResponse
    {
        [JsonProperty(PropertyName = "response")]
        public string Response { get; set; }

        [JsonProperty(PropertyName = "info")]
        public string Info { get; set; }

        public bool Success { get { return Response == "success"; } }
    }

    public class SenderRequest
    {

        [JsonProperty(PropertyName = "request")]
        public string Request { get; set; }
        [JsonProperty(PropertyName = "data")]
        public object[] Data { get; set; }
    }

    public class ItemValue
    {
        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }
        [JsonProperty(PropertyName = "value")]
        public object Value { get; set; }
    }
}