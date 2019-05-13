using CoreOSC;
using System;
using System.Collections.Generic;
using System.Text;
using x_air_Remote.settings;

namespace x_air_Remote.handlers
{
    class MuteHandler
    {
        private UDPDuplex behringer;
        private MuteSetting muteSetting;
        private string mutePath;

        public MuteHandler(UDPDuplex behringer, MuteSetting muteSetting)
        {
            var mutePathBuilder = new StringBuilder();
            mutePathBuilder.Append("/ch/");
            mutePathBuilder.Append(muteSetting.channel.ToString("D2"));
            mutePathBuilder.Append("/mix/on");
            mutePath = mutePathBuilder.ToString();

            this.muteSetting = muteSetting;
            this.behringer = behringer;
        }

        public void mute(bool muted)
        {
            var muteMessage = new CoreOSC.OscMessage(mutePath, muted ? 0 : 1);
            behringer.Send(muteMessage);
        }

    }
}
