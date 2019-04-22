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
        static string _userHash = null;
        static string _xToken = null;

        static AudioFormat _audioFormat = null;
        static VideoFormat _videoFormat = null;
        static AudioFormat _chatAudioFormat = null;

        static bool VerifyIpAddress(string address)
        {
            return System.Net.IPAddress.TryParse(
                address, out System.Net.IPAddress tmp);
        }

        /// <summary>
        /// Authenticate with Xbox Live / refresh tokens
        /// </summary>
        /// <param name="tokenPath">File to json tokenfile</param>
        /// <returns></returns>
        static bool Authenticate(string tokenPath)
        {
            if (String.IsNullOrEmpty(tokenPath))
            {
                return false;
            }

            FileStream fs = new FileStream(tokenPath, FileMode.Open);
            AuthenticationService authenticator = AuthenticationService.LoadFromFile(fs);
            try
            {
                authenticator.Authenticate();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to refresh XBL tokens, error: {e.Message}");
                return false;
            }

            _userHash = authenticator.UserInformation.Userhash;
            _xToken = authenticator.XToken.Jwt;
            return true;
        }

        /// <summary>
        /// Connect to console, request Broadcast Channel and start gamestream
        /// </summary>
        /// <param name="ipAddress">IP address of console</param>
        /// <param name="gamestreamConfig">Desired gamestream configuration</param>
        /// <returns></returns>
        public static async Task<GamestreamSession> ConnectToConsole(string ipAddress, GamestreamConfiguration gamestreamConfig)
        {
            try
            {
                SmartGlassClient client = await SmartGlassClient.ConnectAsync(ipAddress, _userHash, _xToken);
                return await client.BroadcastChannel.StartGamestreamAsync(gamestreamConfig);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection timed out! msg: {e.Message}");
                return null;
            }
        }

        public static async Task<NanoClient> InitNano(string ipAddress, GamestreamSession session)
        {
            NanoClient nano = new NanoClient(ipAddress, session);

            try
            {
                // General Handshaking & Opening channels
                await nano.InitializeProtocolAsync();
                await nano.OpenInputChannelAsync(1280, 720);

                // Audio & Video client handshaking
                // Sets desired AV formats
                _audioFormat = nano.AudioFormats[0];
                _videoFormat = nano.VideoFormats[0];
                await nano.InitializeStreamAsync(_audioFormat, _videoFormat);

                // TODO: Send opus audio chat samples to console
                _chatAudioFormat = new AudioFormat(1, 24000, AudioCodec.Opus);
                await nano.OpenChatAudioChannelAsync(_chatAudioFormat);

                // Tell console to start sending AV frames
                await nano.StartStreamAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to init Nano, error: {e}");
                return null;
            }

            return nano;
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

            if (!Authenticate(tokenPath))
                Console.WriteLine("Connecting anonymously to console, no XBL token available");

            string hostName = ipAddress;

            Console.WriteLine($"Connecting to console {hostName}...");
            GamestreamConfiguration config = GamestreamConfiguration.GetStandardConfig();
            
            GamestreamSession session = ConnectToConsole(ipAddress, config).GetAwaiter().GetResult();
            if (session == null)
            {
                Console.WriteLine("Failed to connect to console!");
                return;
            }

            Console.WriteLine(
                $"Connecting to NANO // TCP: {session.TcpPort}, UDP: {session.UdpPort}");

            NanoClient nano = InitNano(hostName, session).GetAwaiter().GetResult();
            if (nano == null)
            {
                Console.WriteLine("Nano failed!");
                return;
            }

            // SDL / FFMPEG setup
            SdlProducer producer = new SdlProducer(nano, _audioFormat, _videoFormat);

            nano.AudioFrameAvailable += producer.Decoder.ConsumeAudioData;
            nano.VideoFrameAvailable += producer.Decoder.ConsumeVideoData;

            producer.MainLoop();

            // finally (dirty)
            Process.GetCurrentProcess().Kill();
        }
    }
}
