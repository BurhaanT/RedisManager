using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using StackExchange.Redis;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using RedisManager.API.Models;

namespace RedisManager.API.Controllers
{
    public class RedisInstanceController : ApiController
    {
        IConnectionMultiplexer _RedisMUX;
        public RedisInstanceController(IConnectionMultiplexer multiplexConnection)
        {
            _RedisMUX = multiplexConnection;
        }

        public async Task<List<RedisKey>> Keys()
        {
            var endpoints = _RedisMUX.GetEndPoints(true);
            List<RedisKey> keys = new List<RedisKey>();
            foreach (var endpoint in endpoints) 
            {
                var server = _RedisMUX.GetServer(endpoint);
                keys.AddRange(server.Keys());
            }
            return keys;
        }

        public async Task<string> Value(string key)
        {
            var database = _RedisMUX.GetDatabase();
            return await database.StringGetAsync(key);
        }

        public async Task<Dictionary<string, string>> Config()
        {
            Dictionary<string, string> configs = new Dictionary<string, string>();
            var endpoints = _RedisMUX.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = _RedisMUX.GetServer(endpoint);
                foreach (var config in server.ConfigGet())
                {
                    configs.Add(config.Key, config.Value);
                }
            }

            return configs;
        }

        public async Task<List<Dictionary<string, Dictionary<string, string>>>> Info()
        {
            List<Dictionary<string, Dictionary<string, string>>> serverInfoValues = new List<Dictionary<string, Dictionary<string, string>>>();

            var endpoints = _RedisMUX.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = _RedisMUX.GetServer(endpoint);
                var infoDetails = await server.InfoAsync();
                foreach (var infoDetail in infoDetails)
                {
                    var key = infoDetail.Key;
                    Dictionary<string, string> detailsDict = new Dictionary<string, string>();
                    foreach (var infoBreakdown in infoDetail)
                    {
                        var detailsKey = infoBreakdown.Key;
                        var detailsValue = infoBreakdown.Value;
                        if (!detailsKey.IsNullOrWhiteSpace())
                            detailsDict.Add(detailsKey, detailsValue);
                    }
                    Dictionary<string, Dictionary<string, string>> groupDetails =
                        new Dictionary<string, Dictionary<string, string>>();
                    groupDetails.Add(key, detailsDict);
                    serverInfoValues.Add(groupDetails);
                }
            }
            return serverInfoValues;
        }

        public string Status()
        {
            return _RedisMUX.GetStatus();
        }

        public async Task<List<ServerInfo>> Slaves()
        {
            var endpoints = _RedisMUX.GetEndPoints(true);
            List<ServerInfo> slaves = new List<ServerInfo>();

            foreach (var endpoint in endpoints)
            {   
                var server = _RedisMUX.GetServer(endpoint);
                var replicationDetails = await server.InfoAsync("REPLICATION");

                var replicationGroup = replicationDetails.FirstOrDefault();

                if (replicationGroup != null)
                {   
                    var connectedSlaves = int.Parse(replicationGroup.FirstOrDefault(x => x.Key == "connected_slaves").Value);
                    if (connectedSlaves > 0)
                    {
                        for (int i = 0; i < connectedSlaves; i++) //Loop through a of the slaves to get the details
                        {
                            var slaveString = replicationGroup.FirstOrDefault(x => x.Key == $"slave{i}");
                            var slaveDetails = slaveString.Value.Split(',');
                            Dictionary<string, string> serverInfo = slaveDetails.ToDictionary(s => s.Split('=')[0],
                                s => s.Split('=')[1]);
                            slaves.Add(new ServerInfo
                            {
                                IP = serverInfo["ip"],
                                Port = long.Parse(serverInfo["port"]),
                                State = serverInfo["state"]
                            });
                        }
                    }

                }
            }

            return slaves;
        }
    }
}
