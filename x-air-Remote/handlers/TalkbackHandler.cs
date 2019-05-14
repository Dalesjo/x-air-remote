using CoreOSC;
using NLog;
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
        private readonly TalkbackSetting talkbackSettings;
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

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, TalkbackEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(talkbackSettings.gpio, TalkbackDisabled);
            controller.ClosePin(talkbackSettings.gpio);
        }

        public void Talkback(bool talkback)
        {
            log.Info($"Talkback {talkback}");
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