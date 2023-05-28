using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class SickleSlashes : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
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
                        Projectile.NewProjectile(new EntitySource_Buff(npc, Type, buffIndex), npc.Center, Vector2.Zero, ProjectileID.Muramasa, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(25), 0, Main.myPlayer);
                    }
                }
            }
        }
	}
}