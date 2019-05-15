using CoreOSC;
using NLog;
using System;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class DcaHandler
    {
        private readonly static Logger log = LogManager.GetCurrentClassLogger();
        private readonly UDPDuplex behringer;
        private readonly GpioController controller;
        private readonly string dcaPath;
        private readonly DcaSetting dcaSetting;

        public DcaHandler(UDPDuplex behringer, GpioController controller, DcaSetting dcaSetting)
        {
            this.dcaSetting = dcaSetting;
            this.behringer = behringer;
            this.controller = controller;

            var dcaPathBuilder = new StringBuilder();
            dcaPathBuilder.Append("/dca/");
            dcaPathBuilder.Append(dcaSetting.dca.ToString("D1"));
            dcaPathBuilder.Append("/fader");
            dcaPath = dcaPathBuilder.ToString();

            try
            {
                controller.OpenPin(dcaSetting.gpio, PinMode.InputPullUp);
                controller.RegisterCallbackForPinValueChangedEvent(dcaSetting.gpio, PinEventTypes.Falling, dcaDown);
                controller.RegisterCallbackForPinValueChangedEvent(dcaSetting.gpio, PinEventTypes.Rising, dcaUp);
            }
            catch (Exception e)
            {
                log.Info(e,$"Could not connect to pin {dcaSetting.gpio}");
                throw;
            }
        }

        public void Close()
        {
            controller.UnregisterCallbackForPinValueChangedEvent(dcaSetting.gpio, dcaDown);
            controller.UnregisterCallbackForPinValueChangedEvent(dcaSetting.gpio, dcaUp);
            controller.ClosePin(dcaSetting.gpio);
        }

        private void dcaUp(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            dcaLevel(dcaSetting.upLevel);
        }

        private void dcaDown(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            dcaLevel(dcaSetting.downLevel);
        }

        public void dcaLevel(int level)
        {
            log.Info($"GPIO {dcaSetting.gpio} Pressed, SEND {dcaPath},{level}");
            var dcaMessage = new CoreOSC.OscMessage(dcaPath, level);
            behringer.Send(dcaMessage);
        }
    }
}
