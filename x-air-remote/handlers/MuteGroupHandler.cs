using CoreOSC;
using NLog;
using System;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_remote.handlers
{
    class MuteGroupHandler
    {
        private readonly static Logger log = LogManager.GetCurrentClassLogger();
        private readonly UDPDuplex behringer;
        private readonly GpioController controller;
        private readonly string mutePath;
        private readonly MuteGroupSetting muteGroupSetting;
        public MuteGroupHandler(UDPDuplex behringer, GpioController controller, MuteGroupSetting muteGroupSetting)
        {
            this.muteGroupSetting = muteGroupSetting;
            this.behringer = behringer;
            this.controller = controller;

            var mutePathBuilder = new StringBuilder();
            mutePathBuilder.Append("/config/mute/");
            mutePathBuilder.Append(muteGroupSetting.group.ToString("D1"));
            mutePath = mutePathBuilder.ToString();

            try
            {
                controller.OpenPin(muteGroupSetting.gpio, PinMode.InputPullUp);
                controller.RegisterCallbackForPinValueChangedEvent(muteGroupSetting.gpio, PinEventTypes.Falling, MuteEnabled);
                controller.RegisterCallbackForPinValueChangedEvent(muteGroupSetting.gpio, PinEventTypes.Rising, MuteDisabled);
            }
            catch (Exception e)
            {
                log.Info(e, $"Could not connect to pin {muteGroupSetting.gpio}");
                throw;
            }
        }

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(muteGroupSetting.gpio, MuteEnabled);
            controller.UnregisterCallbackForPinValueChangedEvent(muteGroupSetting.gpio, MuteDisabled);
            controller.ClosePin(muteGroupSetting.gpio);
        }

        public void Mute(bool muted)
        {
            log.Info($"GPIO {muteGroupSetting.gpio} Pressed, SEND {mutePath},{muted}");
            var muteMessage = new CoreOSC.OscMessage(mutePath, muted ? 1 : 0);
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
