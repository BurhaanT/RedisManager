using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RedisManager.API.Models
{
    public class ServerInfo
    {
        public  string IP { get; set; }
        public long Port { get; set; }
        public string State { get; set; }

    }
}