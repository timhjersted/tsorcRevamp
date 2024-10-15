using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons.Melee
{
    public class SickleSlashes : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            var player = npc.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerShadowSickle;
            if ((player.statMana > (int)(player.manaCost * 10f)) && !npc.dontTakeDamage)
            {
                if (Main.GameUpdateCount % 30 == 0)
                {
                    player.statMana -= (int)(player.manaCost * 10f);
                    player.manaRegenDelay = 200;
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Slash = Projectile.NewProjectileDirect(npc.GetSource_Buff(buffIndex), npc.Center, Vector2.Zero, ProjectileID.Muramasa, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(ShadowSickle.BaseDamage / 2), 0, player.whoAmI);
                    }
                }
            }
        }
    }
}