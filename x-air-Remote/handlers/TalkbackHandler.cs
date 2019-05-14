using CoreOSC;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class TalkbackHandler
    {

        private readonly UDPDuplex behringer;
        private readonly TalkbackSetting talkbackSettings;
        private readonly string talkbackPath;
        private readonly GpioController controller;

        public TalkbackHandler(UDPDuplex behringer, GpioController controller, TalkbackSetting talkbackSetting)
        {
            this.talkbackSettings = talkbackSetting;
            this.behringer = behringer;
            this.controller = controller;

            var talkbackPathBuilder = new StringBuilder();
            talkbackPathBuilder.Append("/ch/");
            talkbackPathBuilder.Append(talkbackSetting.channel.ToString("D2"));
            talkbackPathBuilder.Append("/mix/");
            talkbackPathBuilder.Append(talkbackSetting.bus.ToString("D2"));
            talkbackPathBuilder.Append("/tap");
            talkbackPath = talkbackPathBuilder.ToString();

            controller.OpenPin(talkbackSettings.gpio, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, PinEventTypes.Falling, TalkbackEnabled);
            controller.RegisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, PinEventTypes.Rising, TalkbackDisabled);
        }

        private void TalkbackDisabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Talkback(false);
        }

        private void TalkbackEnabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Talkback(true);
        }

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, TalkbackEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, TalkbackDisabled);
            controller.ClosePin(talkbackSettings.gpio);
        }

        public void Talkback(bool talkback)
        {
            var talkbackMessage = new CoreOSC.OscMessage(talkbackPath, talkback ? 3 : 4);
            behringer.Send(talkbackMessage);
        }
    }
}
