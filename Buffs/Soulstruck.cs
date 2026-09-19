using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.GreatSoulArrowStaff;
using tsorcRevamp.NPCs;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs
{
    public class Soulstruck : ModBuff
    {
        //Generic texture since this buff is enemy-only
        public override string Texture => "Terraria/Images/Buff";
        
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true; 
            tsorcFactory.NonWhipTagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<GreatSoulArrowStaffNPC>().IsSoulstruck = true;
        }
    }
}
