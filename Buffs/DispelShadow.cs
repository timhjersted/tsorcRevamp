using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class DispelShadow : ModBuff
    {

        //Generic texture since this buff is enemy-only
        public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dispel Shadow");
            // Description.SetDefault("Your defense has been dispelled");
        }
    }
}
