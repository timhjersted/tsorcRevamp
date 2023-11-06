using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;

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
            var player = Main.LocalPlayer;
            if (player.GetModPlayer<tsorcRevampPlayer>().HasShadowSickle)
            {
                if (player.statMana > (int)(player.manaCost * 10f))
                {
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        player.statMana -= (int)(player.manaCost * 10f);
                        player.manaRegenDelay = 200;
                        Projectile Slash = Projectile.NewProjectileDirect(npc.GetSource_Buff(buffIndex), npc.Center, Vector2.Zero, ProjectileID.Muramasa, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(ShadowSickle.BaseDamage / 2), 0, Main.myPlayer);
                        Slash.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + player.HeldItem.crit;
                        Slash.netUpdate = true;
                    }
                }
            }
        }
    }
}