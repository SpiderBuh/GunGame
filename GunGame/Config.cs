namespace GunGame
{
    public class Config
    {
        //Automatically starts GunGame at the start of each round (highly reccomended in it's current state)
        public bool AutoGunGame { get; set; } = true;

        //Allow friendly fire in the team gamemode
        public bool TeamsFriendlyFire { get; set; } = false;
        
        //Punish friendly fire (if enabled) in the team gamemode
        public bool PunishTeamFF { get; set; } = false;
        
        //Punish deaths to self and scenarios with no killer, e.g. tesla gate
        public bool PunishAccident { get; set; } = false;
        
        //Block fall damage
        public bool BlockFallDamage { get; set; } = true;
        
        //Retain the kills and scores of someone who leaves and joins back in the same round
        public bool RetainScoreAfterRejoin { get; set; } = true;

        //Maximum number of bodies on the map at once
        public uint MaxBodies { get; set; } = 75;

        //Maximum number of kills needed to win (scales with amount of players)
        public byte MaxStages { get; set; } = 30;
    }
}
