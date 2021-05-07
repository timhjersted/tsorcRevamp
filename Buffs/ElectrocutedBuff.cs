using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs
{
    public class ElectrocutedBuff : ModBuff
    {
        public override bool Autoload(ref string name, ref string texture)
        {
            // NPC only buff so we'll just assign it a useless buff icon.
            texture = "tsorcRevamp/Buffs/ArmorDrug";
            return base.Autoload(ref name, ref texture);
        }
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Electrocuted!");
            Description.SetDefault("Losing life");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            longerExpertDebuff = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ElectrocutedEffect = true;
        }
    }
}
