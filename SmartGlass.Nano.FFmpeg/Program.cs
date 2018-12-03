using System;
using System.IO;
using System.Threading.Tasks;
using SmartGlass.Channels;
using SmartGlass.Channels.Broadcast;
using SmartGlass.Common;
using SmartGlass.Nano;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;
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
            GamestreamConfiguration config = GamestreamConfiguration.GetStandardConfig();

            SmartGlassClient client = null;
            try
            {
                client = SmartGlassClient.ConnectAsync(hostName, userHash, xToken)
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection timed out! msg: {0}", e);
                return;
            }

            GamestreamSession session = client.BroadcastChannel.StartGamestreamAsync(config)
                .GetAwaiter().GetResult();

            Console.WriteLine(
                $"Connecting to NANO // TCP: {session.TcpPort}, UDP: {session.UdpPort}");

            NanoClient nano = new NanoClient(hostName, session);

            // General Handshaking & Opening channels
            nano.InitializeProtocolAsync()
                .GetAwaiter().GetResult();

            nano.OpenInputChannelAsync(1280, 720)
                .GetAwaiter().GetResult();

            // Audio & Video client handshaking
            // Sets desired AV formats
            AudioFormat audioFormat = nano.AudioFormats[0];
            VideoFormat videoFormat = nano.VideoFormats[0];
            nano.InitializeStreamAsync(audioFormat, videoFormat)
                .GetAwaiter().GetResult();

            // TODO: Send opus audio chat samples to console
            AudioFormat chatAudioFormat = new AudioFormat(1, 24000, AudioCodec.Opus);
            nano.OpenChatAudioChannelAsync(chatAudioFormat)
                .GetAwaiter().GetResult();

            FFmpegConsumer consumer = new FFmpegConsumer(audioFormat, videoFormat);
            nano.AddConsumer(consumer);

            // Start consumer to get decoding threads running
            consumer.Start();

            // Tell console to start sending AV frames
            nano.StartStreamAsync()
                .GetAwaiter().GetResult();

            consumer.MainLoop();
        }
    }
}
