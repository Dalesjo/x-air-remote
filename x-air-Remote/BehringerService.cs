using CoreOSC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using x_air_Remote.handlers;
using x_air_Remote.settings;

namespace x_air_Remote
{
    public class BehringerService : BackgroundService
    {

        private readonly ILogger<BehringerService> log;
        readonly private IConfiguration configuration;
        private int counter = 0;
        private UDPDuplex behringer;
        private HandleOscPacket callback;

        private string host;
        private int port;
        private int clientPort;
        private List<MuteSetting> muteSettings;
        private List<TallySetting> tallySettings;
        private List<TalkbackSetting> talkbackSettings;

        private List<TallyHandler> tallyHandlers;

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

        public void logMessage(OscPacket packet)
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
            callback += logMessage;
            return Task.Run(() =>
            {
                log.LogInformation("Connected to Behringer IP: {host} Port: {port}");
                behringer = new UDPDuplex(host, port, clientPort, callback);
                new Thread(() => KeepAlive()) { IsBackground = true }.Start();

                foreach (var tallySetting in tallySettings)
                {
                    var handler = new TallyHandler(behringer, tallySetting);
                    tallyHandlers.Add(handler);
                }

                cancellationToken.WaitHandle.WaitOne();
                log.LogInformation("ClipStatsService terminated");
            });
        }
    }
}