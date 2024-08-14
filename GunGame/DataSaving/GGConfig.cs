using System;

namespace GunGame.DataSaving
{
    public class GGConfig : ConfigObject
    {
        public override string FileName => "GunGameConfig.xml";
        public override DataWrapper Wrapper => Options;
        public GGConfigOptions Options { get; private set; }
        public GGConfig() { Options = GunGameDataManager.LoadData<GGConfigOptions>(FileName); }
        public GGConfig(GGConfigOptions d)
        {
            Options = d;
        }

        [Serializable]
        public class GGConfigOptions : DataWrapper 
        {
            public bool TeamsFriendlyFire;
            public bool PunishTeamFF;
            //TODO:
            //playable zones
            //max stages
        }
    }
}
