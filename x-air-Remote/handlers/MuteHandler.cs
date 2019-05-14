using CoreOSC;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class MuteHandler
    {
        private readonly UDPDuplex behringer;
        private readonly MuteSetting muteSetting;
        private readonly string mutePath;
        private readonly GpioController controller;

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

            controller.OpenPin(muteSetting.gpio, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(muteSetting.gpio, PinEventTypes.Falling, MuteEnabled);
            controller.RegisterCallbackForPinValueChangedEvent(muteSetting.gpio, PinEventTypes.Rising, MuteDisabled);
        }

        private void MuteDisabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Mute(true);
        }

        private void MuteEnabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Mute(false);
        }

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(muteSetting.gpio, MuteEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(muteSetting.gpio, MuteDisabled);
            controller.ClosePin(muteSetting.gpio);
        }

        public void Mute(bool muted)
        {
            var muteMessage = new CoreOSC.OscMessage(mutePath, muted ? 0 : 1);
            behringer.Send(muteMessage);
        }

    }
}
