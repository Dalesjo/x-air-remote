﻿using CoreOSC;
using NLog;
using System;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    internal class MuteHandler
    {
        private readonly static Logger log = LogManager.GetCurrentClassLogger();
        private readonly UDPDuplex behringer;
        private readonly GpioController controller;
        private readonly string mutePath;
        private readonly MuteSetting muteSetting;
        public MuteHandler(UDPDuplex behringer, GpioController controller, MuteSetting muteSetting)
        {
            this.muteSetting = muteSetting;
            this.behringer = behringer;
            this.controller = controller;

            var mutePathBuilder = new StringBuilder();
            mutePathBuilder.Append("/ch/");
            mutePathBuilder.Append(muteSetting.channel.ToString("D2"));
            mutePathBuilder.Append("/mix/on");
            mutePath = mutePathBuilder.ToString();

            try
            {
                controller.OpenPin(muteSetting.gpio, PinMode.InputPullUp);
                controller.RegisterCallbackForPinValueChangedEvent(muteSetting.gpio, PinEventTypes.Falling, MuteEnabled);
                controller.RegisterCallbackForPinValueChangedEvent(muteSetting.gpio, PinEventTypes.Rising, MuteDisabled);
            }
            catch (Exception e)
            {
                log.Info(e,$"Could not connect to pin {muteSetting.gpio}");
                throw;
            }
}

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(muteSetting.gpio, MuteEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(muteSetting.gpio, MuteDisabled);
            controller.ClosePin(muteSetting.gpio);
        }

        public void Mute(bool muted)
        {
            log.Info($"GPIO {muteSetting.gpio} Pressed, SEND {mutePath},{muted}");
            var muteMessage = new CoreOSC.OscMessage(mutePath, muted ? 0 : 1);
            behringer.Send(muteMessage);
        }

        private void MuteDisabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Mute(false);
        }

        private void MuteEnabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Mute(true);
        }
    }
}