﻿using CoreOSC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;
using x_air_remote.handlers;
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
        private readonly List<MuteGroupHandler> muteGroupHandlers;
        private readonly List<MuteGroupSetting> muteGroupSettings;
        private readonly int port;
        private readonly List<TalkbackHandler> talkbackHandlers;
        private readonly List<TalkbackSetting> talkbackSettings;
        private readonly List<TallyHandler> tallyHandlers;
        private readonly List<TallySetting> tallySettings;
        private readonly List<DcaHandler> dcaHandlers;
        private readonly List<DcaSetting> dcaSettings;

        private UDPDuplex behringer;
        private HandleOscPacket callback;
        private int counter = 0;
        public BehringerService(IConfiguration configuration, ILogger<BehringerService> logger)
        {
            log = logger;
            this.configuration = configuration;
            counter = 0;
            log.LogInformation("BehringerService started");

            log.LogInformation("Reading Logfiles");
            host = configuration.GetValue<string>("host");
            port = configuration.GetValue<int>("port");
            clientPort = configuration.GetValue<int>("clientPort");

            muteSettings = configuration.GetSection("mute").Get<List<MuteSetting>>();
            muteGroupSettings = configuration.GetSection("muteGroup").Get<List<MuteGroupSetting>>();
            tallySettings = configuration.GetSection("tally").Get<List<TallySetting>>();
            talkbackSettings = configuration.GetSection("talkback").Get<List<TalkbackSetting>>();
            dcaSettings = configuration.GetSection("dca").Get<List<DcaSetting>>();

            tallyHandlers = new List<TallyHandler>();
            muteHandlers = new List<MuteHandler>();
            muteGroupHandlers = new List<MuteGroupHandler>();
            talkbackHandlers = new List<TalkbackHandler>();
            dcaHandlers = new List<DcaHandler>();

        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            /**
              * Callback handle that will handle
              * every message recieved from Behringer X Air */
            return Task.Run(() =>
            {
                try
                {
                    log.LogInformation($"Connected to Behringer IP: {host} Port: {port} over UDP");
                    behringer = new UDPDuplex(host, port, clientPort, callback);

                    log.LogInformation($"Connecting to GpioController");
                    GpioController controller = new GpioController(PinNumberingScheme.Logical);

                    log.LogInformation($"Starting Background Thread for keepalive");
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

                    if (muteGroupSettings is List<MuteGroupSetting>)
                    {
                        foreach (var muteGroupSetting in muteGroupSettings)
                        {
                            var handler = new MuteGroupHandler(behringer, controller, muteGroupSetting);
                            muteGroupHandlers.Add(handler);
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

                    if (dcaSettings is List<DcaSetting>)
                    {
                        foreach (var dcaSetting in dcaSettings)
                        {
                            var handler = new DcaHandler(behringer, controller, dcaSetting);
                            dcaHandlers.Add(handler);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "BehringerService crashed while running");
                    throw;
                }

                cancellationToken.WaitHandle.WaitOne();

                try
                {
                    foreach (var handler in tallyHandlers)
                    {
                        handler.Close();
                    }

                    foreach (var handler in muteHandlers)
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

                    foreach (var handler in dcaHandlers)
                    {
                        handler.Close();
                    }

                    log.LogInformation("UDP connection closed.");
                    behringer.Close();
                }
                catch (Exception e)
                {
                    log.LogError(e, "BehringerService crashed while terminating");
                    throw;
                }
            });
        }

        private void KeepAlive()
        {
            while (true)
            {
                var message = new CoreOSC.OscMessage("/xremote");
                log.LogInformation($"Command sent: {message}");
                behringer.Send(message);
                Thread.Sleep(8000);
            }
        }
    }
}