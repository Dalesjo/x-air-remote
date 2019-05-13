using CoreOSC;
using System;
using System.Collections.Generic;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class TallyHandler
    {
        private UDPDuplex behringer;
        private TallySetting tallySetting;
        private double level;
        private bool muted;

        private string levelPath;
        private string mutePath;

        public TallyHandler(UDPDuplex behringer, TallySetting tallySetting)
        {
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
            behringer.OscPacketCallback += callback;
            forceUpdate();
        }

        private void checkStatus()
        {
            if(level > tallySetting.level && muted == false)
            {
                Console.WriteLine($"TallyHandler {tallySetting.channel.ToString("D2")} ON");
            }
            else
            {
                Console.WriteLine($"TallyHandler {tallySetting.channel.ToString("D2")} OFF");
            }
        }

        private void forceUpdate()
        {
            var forceLevelUpdate = new CoreOSC.OscMessage(levelPath);
            behringer.Send(forceLevelUpdate);

            var forceMuteUpdate = new CoreOSC.OscMessage(mutePath);
            behringer.Send(forceMuteUpdate);
        }

        public void callback(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            

            if (messageReceived.Address == levelPath && messageReceived.Arguments.Count > 0)
            {
                level = Convert.ToDouble(messageReceived.Arguments[0]);
                checkStatus();
            }
            else if (messageReceived.Address == mutePath && messageReceived.Arguments.Count > 0)
            {
                muted = !Convert.ToBoolean(messageReceived.Arguments[0]);
                checkStatus();
            }
        }
    }
}
