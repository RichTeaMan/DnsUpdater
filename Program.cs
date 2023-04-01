using System.Net;
using System.Diagnostics;

namespace DnsUpdater
{
    public static class Program
    {
        private static string[] ArgsByPrefix(string[] args, string prefix)
        {
            return args.Where(a => a.StartsWith(prefix + "=")).Select(a => a.Replace(prefix + "=", string.Empty)).ToArray();
        }

        public async static Task<int> Main(string[] args)
        {
            Console.WriteLine("DNS Updater.");


            try
            {
                await RunDnsUpdater(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
                return -1;
            }

        }

        private static async Task RunDnsUpdater(string[] args)
        {
            string? uiPassword = ArgsByPrefix(args, "ui-password")?.FirstOrDefault();

            string[] hosts = ArgsByPrefix(args, "host");
            string ipAddress = GetLocalIPAddress();

            if (string.IsNullOrEmpty(uiPassword))
            {
                throw new Exception("ui-password must be set as a command line parameter.");
            }

            if (hosts == null || hosts.Length == 0)
            {
                throw new Exception("host must be set as a command line parameter.");
            }

            using var api = new Api("https://pihole.internal/", uiPassword);

            string token = await api.FetchToken();
            var dns = await api.GetDns(token);

            var hostsToUpdate = new List<string>();
            // there appears to be a bug wherein removing a DNS entry that is a prefix of another
            // entry removes both entries. Eg, deleting 'aaa' will also remove 'aaa.internal'.
            // order by length desc hacks around this by deleting longest names first.
            foreach (var host in hosts.OrderByDescending(h => h.Length))
            {
                if (dns.ContainsKey(host))
                {
                    string currentIp = dns[host];
                    if (currentIp != ipAddress)
                    {
                        await api.DeleteDns(host, currentIp, token);
                        hostsToUpdate.Add(host);
                    }
                    else
                    {
                        Console.WriteLine($"Host {host} already has record {ipAddress}");
                    }
                }
                else
                {
                    hostsToUpdate.Add(host);
                }
            }

            foreach (var host in hostsToUpdate)
            {
                Debug.WriteLine($"Updating {host} -> {ipAddress}");
                await api.UpdateDns(host, ipAddress, token);
                Console.WriteLine($"Updated {host} -> {ipAddress}");
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.ToString().StartsWith("192"))
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
