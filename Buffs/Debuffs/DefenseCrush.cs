using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class DefenseCrush : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; 
            Main.buffNoSave[Type] = true; 
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<DefenseCrushGlobalNPC>().affectedByDefenseCrush = true;
        }
    }

    public class DefenseCrushGlobalNPC : GlobalNPC
    {
        public bool affectedByDefenseCrush;

        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            affectedByDefenseCrush = false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (affectedByDefenseCrush)
            {
                modifiers.Defense -= 15; 
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (affectedByDefenseCrush)
            {
                modifiers.Defense -= 15; 
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (affectedByDefenseCrush)
            {
                drawColor = Color.Lerp(drawColor, Color.Gray, 0.33f);
            }
        }
    }
}