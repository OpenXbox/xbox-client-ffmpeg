using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NDesk.Options;

using SmartGlass.Channels;
using SmartGlass.Channels.Broadcast;
using SmartGlass.Common;
using SmartGlass.Nano;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;
using XboxWebApi.Authentication;

using SmartGlass.Nano.FFmpeg.Producer;
using System.Collections.Generic;

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
            var printHelp = false;
            var ipAddress = String.Empty;
            var tokenPath = String.Empty;

            var p = new OptionSet {
                { "h|?|help", "Show this help and exit", v => printHelp = v != null },
                { "a|address=", "Specify {IP ADDRESS} of target console", v =>
                {
                    if (!VerifyIpAddress(v))
                        throw new OptionException("Invalid IP Address", "address");
                    ipAddress = v;
                }},
                { "t|token=", "Specify {TOKEN FILEPATH} for connecting authenticated", v =>
                {
                    if (!File.Exists(v))
                        throw new OptionException("Invalid tokenpath", "token");
                    tokenPath = v;
                }}
            };

            List<string> extraArgs;
            try
            {
                extraArgs = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Failed parsing parameter \'{e.OptionName}\': {e.Message}");
                Console.WriteLine("Try 'SmartGlass.Nano.FFmpeg --help' for more information");
                return;
            }

            if (printHelp || String.IsNullOrEmpty(ipAddress))
            {
                Console.WriteLine("Usage  : SmartGlass.Nano.FFmpeg [parameters]");
                Console.WriteLine("Gamestream from xbox one");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            string userHash = null;
            string xToken = null;

            if (!String.IsNullOrEmpty(tokenPath))
            {
                // Authenticate with Xbox Live
                FileStream fs = new FileStream(tokenPath, FileMode.Open);
                AuthenticationService authenticator = AuthenticationService.LoadFromFile(fs);
                try
                {
                    authenticator.Authenticate();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to refresh XBL tokens, error: {e.Message}");
                    return;
                }
                userHash = authenticator.UserInformation.Userhash;
                xToken = authenticator.XToken.Jwt;
            }

            string hostName = ipAddress;

            Console.WriteLine($"Connecting to console {hostName}...");
            GamestreamConfiguration config = GamestreamConfiguration.GetStandardConfig();

            SmartGlassClient client = null;
            try
            {
                client = SmartGlassClient.ConnectAsync(hostName, userHash, xToken)
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection timed out! msg: {e.Message}");
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

            // Tell console to start sending AV frames
            nano.StartStreamAsync().GetAwaiter().GetResult();

            // SDL / FFMPEG setup
            SdlProducer producer = new SdlProducer(nano, audioFormat, videoFormat);

            nano.AudioFrameAvailable += producer.Decoder.ConsumeAudioData;
            nano.VideoFrameAvailable += producer.Decoder.ConsumeVideoData;

            producer.MainLoop();

            // finally (dirty)
            Process.GetCurrentProcess().Kill();
        }
    }
}
