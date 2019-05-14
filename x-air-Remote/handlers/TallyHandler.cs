using CoreOSC;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class TallyHandler
    {
        private readonly UDPDuplex behringer;
        private readonly TallySetting tallySetting;
        private readonly GpioController controller;

        private double level;
        private bool muted;

        private readonly string levelPath;
        private readonly string mutePath;

        public TallyHandler(UDPDuplex behringer, GpioController controller, TallySetting tallySetting)
        {
            controller.OpenPin(tallySetting.gpio, PinMode.Output);

            var levelPathBuilder = new StringBuilder();
            levelPathBuilder.Append("/ch/");
            levelPathBuilder.Append(tallySetting.channel.ToString("D2"));
            levelPathBuilder.Append("/mix/fader");
            levelPath = levelPathBuilder.ToString();

            var mutePathBuilder = new StringBuilder();
            mutePathBuilder.Append("/ch/");
            mutePathBuilder.Append(tallySetting.channel.ToString("D2"));
            mutePathBuilder.Append("/mix/on");
            mutePath = mutePathBuilder.ToString();

            level = 0;
            muted = true;

            this.tallySetting = tallySetting;
            this.behringer = behringer;
            this.controller = controller;

            behringer.OscPacketCallback += Callback;
            ForceUpdate();
        }

        public void Close()
        {
            controller.ClosePin(tallySetting.gpio);
        }

        private void CheckStatus()
        {
            if(level > tallySetting.level && muted == false)
            {
                Console.WriteLine($"TallyHandler {tallySetting.channel.ToString("D2")} ON");
                controller.Write(tallySetting.gpio, PinValue.High);
            }
            else
            {
                Console.WriteLine($"TallyHandler {tallySetting.channel.ToString("D2")} OFF");
                controller.Write(tallySetting.gpio, PinValue.Low);
            }
        }

        private void ForceUpdate()
        {
            var forceLevelUpdate = new CoreOSC.OscMessage(levelPath);
            behringer.Send(forceLevelUpdate);

            var forceMuteUpdate = new CoreOSC.OscMessage(mutePath);
            behringer.Send(forceMuteUpdate);
        }

        public void Callback(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            
            if (messageReceived.Address == levelPath && messageReceived.Arguments.Count > 0)
            {
                level = Convert.ToDouble(messageReceived.Arguments[0]);
                CheckStatus();
            }
            else if (messageReceived.Address == mutePath && messageReceived.Arguments.Count > 0)
            {
                muted = !Convert.ToBoolean(messageReceived.Arguments[0]);
                CheckStatus();
            }
        }
    }
}
