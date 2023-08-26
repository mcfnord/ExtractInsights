using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExtractInsights
{
    class VisibleNow
    {

        public static Dictionary<string, string> JamulusListURLs = new Dictionary<string, string>()
        {
            { "Any Genre 1", "http://143.198.104.205/servers.php?central=anygenre1.jamulus.io:22124" }
            , { "Any Genre 2", "http://143.198.104.205/servers.php?central=anygenre2.jamulus.io:22224" }
            , { "Any Genre 3", "http://143.198.104.205/servers.php?central=anygenre3.jamulus.io:22624" }
            , { "Genre Rock", "http://143.198.104.205/servers.php?central=rock.jamulus.io:22424" }
            , { "Genre Jazz", "http://143.198.104.205/servers.php?central=jazz.jamulus.io:22324" }
            , { "Genre Classical/Folk", "http://143.198.104.205/servers.php?central=classical.jamulus.io:22524" }
            , { "Genre Choral/BBShop", "http://143.198.104.205/servers.php?central=choral.jamulus.io:22724" }
        };

        public class Client
        {
            public long chanid { get; set; }
            public string country { get; set; }
            public string instrument { get; set; }
            public string skill { get; set; }
            public string name { get; set; }
            public string city { get; set; }
        }

        public enum Os { Linux, MacOs, Windows };


        public class JamulusServers
        {
            public long numip { get; set; }
            public long port { get; set; }
            public string? country { get; set; }
            public long maxclients { get; set; }
            public long perm { get; set; }
            public string name { get; set; }
            public string ipaddrs { get; set; }
            public string city { get; set; }
            public string ip { get; set; }
            public long ping { get; set; }
            public Os ps { get; set; }
            public string version { get; set; }
            public string versionsort { get; set; }
            public long nclients { get; set; }
            public long index { get; set; }
            public Client[] clients { get; set; }
            public long port2 { get; set; }
        }


        public static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        public static string GetHash(string name, string country, string instrument)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(name + country + instrument);
            var hashOfGuy = System.Security.Cryptography.MD5.HashData(bytes);
            //var h = System.Convert.ToBase64String(hashOfGuy);
            var h = ToHex(hashOfGuy, false);
            //            m_guidNamePairs[h] = System.Web.HttpUtility.HtmlEncode(name); // This is the name map for JammerMap
            return h;
        }

        public static Dictionary<string, string> LastReportedList = new Dictionary<string, string>();

        public static bool UserVisibleNow(string personGuid)
        {
            foreach (var key in JamulusListURLs.Keys)
            {
                string bigKey = FindPatterns.MinuteSince2023AsInt().ToString() + key;

                if (false == LastReportedList.ContainsKey(bigKey))
                {
                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, ssl) => { return true; };
                    using var client = new HttpClient(httpClientHandler);
                    var g = client.GetStringAsync(JamulusListURLs[key]);
                    g.Wait(); // wait for data to arrive
                    var newReportedList = g.Result; // only proceeds when data arrives

                    LastReportedList[bigKey] = newReportedList;
                }

                var serversOnList = System.Text.Json.JsonSerializer.Deserialize<List<JamulusServers>>(LastReportedList[bigKey]);

                foreach (var server in serversOnList)
                {
                    if (server.clients != null)
                    {
                        foreach (var guy in server.clients)
                        {
                            string stringHashOfGuy = GetHash(guy.name, guy.country, guy.instrument);
                            if (stringHashOfGuy == personGuid)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
