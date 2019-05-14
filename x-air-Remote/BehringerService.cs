using CoreOSC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;
using x_air_Remote.handlers;
using x_air_Remote.settings;

namespace x_air_Remote
{
    public class BehringerService : BackgroundService
    {
        private readonly int clientPort;
        readonly private IConfiguration configuration;
        private readonly string host;
        private readonly ILogger<BehringerService> log;
        private readonly List<MuteHandler> muteHandlers;
        private readonly List<MuteSetting> muteSettings;
        private readonly int port;
        private readonly List<TalkbackHandler> talkbackHandlers;
        private readonly List<TalkbackSetting> talkbackSettings;
        private readonly List<TallyHandler> tallyHandlers;
        private readonly List<TallySetting> tallySettings;
        private UDPDuplex behringer;
        private HandleOscPacket callback;
        private int counter = 0;
        public BehringerService(IConfiguration configuration, ILogger<BehringerService> logger)
        {
            log = logger;
            this.configuration = configuration;
            counter = 0;
            log.LogInformation("BehringerService started");

            host = configuration.GetValue<string>("host");
            port = configuration.GetValue<int>("port");
            clientPort = configuration.GetValue<int>("clientPort");

            muteSettings = configuration.GetSection("mute").Get<List<MuteSetting>>();
            tallySettings = configuration.GetSection("tally").Get<List<TallySetting>>();
            talkbackSettings = configuration.GetSection("talkback").Get<List<TalkbackSetting>>();

            tallyHandlers = new List<TallyHandler>();
            muteHandlers = new List<MuteHandler>();
            talkbackHandlers = new List<TalkbackHandler>();
        }

        public void LogMessage(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            Console.Write(++counter + "#");
            Console.Write(messageReceived.Address.ToString());
            foreach (var arg in messageReceived.Arguments)
            {
                Console.Write("," + arg.ToString());
            }

            Console.WriteLine("");
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            /**
              * Callback handle that will handle
              * every message recieved from Behringer X Air */
            callback += LogMessage;
            return Task.Run(() =>
            {
                log.LogInformation("Connected to Behringer IP: {host} Port: {port}");
                behringer = new UDPDuplex(host, port, clientPort, callback);
                GpioController controller = new GpioController(PinNumberingScheme.Logical);

                new Thread(() => KeepAlive()) { IsBackground = true }.Start();

                if (tallySettings is List<TallySetting>)
                {
                    foreach (var tallySetting in tallySettings)
                    {
                        var handler = new TallyHandler(behringer, controller, tallySetting);
                        tallyHandlers.Add(handler);
                    }
                }

                if (muteSettings is List<MuteSetting>)
                {
                    foreach (var muteSetting in muteSettings)
                    {
                        var handler = new MuteHandler(behringer, controller, muteSetting);
                        muteHandlers.Add(handler);
                    }
                }

                if (talkbackSettings is List<TalkbackSetting>)
                {
                    foreach (var talkbackSetting in talkbackSettings)
                    {
                        var handler = new TalkbackHandler(behringer, controller, talkbackSetting);
                        talkbackHandlers.Add(handler);
                    }
                }

                cancellationToken.WaitHandle.WaitOne();

                foreach (var handler in tallyHandlers)
                {
                    handler.Close();
                }

                foreach (var handler in muteHandlers)
                {
                    handler.Close();
                }

                foreach (var handler in talkbackHandlers)
                {
                    handler.Close();
                }

                log.LogInformation("BehringerService terminated");
            });
        }

        private void KeepAlive()
        {
            while (true)
            {
                var message = new CoreOSC.OscMessage("/xremote");
                log.LogInformation($"command sent: {message}");
                behringer.Send(message);
                Thread.Sleep(8000);
            }
        }
    }
}