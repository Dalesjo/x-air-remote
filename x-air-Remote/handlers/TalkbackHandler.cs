using CoreOSC;
using NLog;
using System;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    internal class TalkbackHandler
    {
        private readonly static Logger log = LogManager.GetCurrentClassLogger();
        private readonly UDPDuplex behringer;
        private readonly GpioController controller;
        private readonly string talkbackPath;
        private readonly TalkbackSetting talkbackSetting;
        public TalkbackHandler(UDPDuplex behringer, GpioController controller, TalkbackSetting talkbackSetting)
        {
            this.talkbackSetting = talkbackSetting;
            this.behringer = behringer;
            this.controller = controller;

            var talkbackPathBuilder = new StringBuilder();
            talkbackPathBuilder.Append("/ch/");
            talkbackPathBuilder.Append(talkbackSetting.channel.ToString("D2"));
            talkbackPathBuilder.Append("/mix/");
            talkbackPathBuilder.Append(talkbackSetting.bus.ToString("D2"));
            talkbackPathBuilder.Append("/tap");
            talkbackPath = talkbackPathBuilder.ToString();

            try
            {
                controller.OpenPin(talkbackSetting.gpio, PinMode.InputPullUp);
                controller.RegisterCallbackForPinValueChangedEvent(talkbackSetting.gpio, PinEventTypes.Falling, TalkbackEnabled);
                controller.RegisterCallbackForPinValueChangedEvent(talkbackSetting.gpio, PinEventTypes.Rising, TalkbackDisabled);
            }
            catch (Exception e)
            {
                log.Info(e,$"Could not connect to pin {talkbackSetting.gpio}");
                throw;
            }
}

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSetting.gpio, TalkbackEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSetting.gpio, TalkbackDisabled);
            controller.ClosePin(talkbackSetting.gpio);
        }

        public void Talkback(bool talkback)
        {
            log.Info($"GPIO {talkbackSetting.gpio} Pressed, SEND {talkbackPath},{talkback}");
            var talkbackMessage = new CoreOSC.OscMessage(talkbackPath, talkback ? 3 : 4);
            behringer.Send(talkbackMessage);
        }

        private void TalkbackDisabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Talkback(false);
        }

        private void TalkbackEnabled(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Talkback(true);
        }
    }
}