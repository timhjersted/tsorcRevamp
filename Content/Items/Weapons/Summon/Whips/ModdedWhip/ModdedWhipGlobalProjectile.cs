using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.ModdedWhip
{
    public class ModdedWhipGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        
        public bool IsModded;
        public bool CanBeCharged;
    }
}

