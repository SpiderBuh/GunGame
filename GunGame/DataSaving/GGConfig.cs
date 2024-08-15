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
            public GGConfigOptions() { }
            public bool TeamsFriendlyFire { get; set; } = false;
            public bool PunishTeamFF { get; set; } = false;
            public bool PunishAccident { get; set; } = false;
            public bool BlockFallDamage { get; set; } = true;
            public uint MaxBodies { get; set; } = 75;
            public byte MaxStages { get; set; } = 30;
            //TODO:
            //playable zones
        }
    }
}
