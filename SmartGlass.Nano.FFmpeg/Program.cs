using System;
using System.IO;
using System.Threading.Tasks;
using SmartGlass.Channels;
using SmartGlass.Channels.Broadcast;
using SmartGlass.Nano;
using SmartGlass.Nano.Consumer;
using XboxWebApi.Authentication;

namespace SmartGlass.Nano.FFmpeg
{
    class Program
    {
        static bool VerifyIpAddress(string address)
        {
            return System.Net.IPAddress.TryParse(
                address, out System.Net.IPAddress tmp);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Error: Need an IP address!");
                return;
            }

            if (!VerifyIpAddress(args[0]))
            {
                Console.WriteLine("Passed IP argument is invalid {0}", args[0]);
                return;
            }

            string userHash = null;
            string xToken = null;

            if (args.Length == 2)
            {
                // Authenticate with Xbox Live
                FileStream fs = null;
                string tokenPath = args[1];
                try
                {
                    fs = new FileStream(tokenPath, FileMode.Open);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load tokens from \'{0}\', error: {1}",
                        tokenPath, e.Message);
                    return;
                }

                AuthenticationService authenticator = AuthenticationService
                                                        .LoadFromFile(fs);
                try
                {
                    authenticator.Authenticate();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to refresh XBL tokens, error: {0}", e.Message);
                    return;
                }
                userHash = authenticator.UserInformation.Userhash;
                xToken = authenticator.XToken.Jwt;
            }

            string hostName = args[0];

            Console.WriteLine("Connecting to console {0}...", hostName);
            FFmpegConsumer consumer = new FFmpegConsumer();

            try
            {
                StartStream(hostName, userHash, xToken, consumer).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection timed out! msg: {0}", e);
                return;
            }
            consumer.Start();
            consumer.MainLoop();
        }

        static async Task StartStream(string hostName, string userHash, string xToken,
                                        IConsumer consumer)
        {
            SmartGlassClient client = await SmartGlassClient
                .ConnectAsync(hostName, userHash, xToken);

            GamestreamSession sess = await client.BroadcastChannel
                .StartGamestreamAsync();

            Console.WriteLine(
                $"Connecting to TCP: {sess.TcpPort}, UDP: {sess.UdpPort}");

            NanoClient nano = new NanoClient(hostName,
                                         sess.TcpPort, sess.UdpPort,
                                         new System.Guid(),
                                         consumer);
            await nano.Initialize();
            await nano.StartStream();
        }
    }
}
