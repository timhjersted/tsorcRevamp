using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Buffs.Summon
{
	public class TerraFallBuff : ModBuff
	{
        public float AttackSpeed;
        public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}
		public override void Update(Player player, ref int buffIndex)
        {
            int WhipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(TerraFall.BaseDamage);
            AttackSpeed = player.GetModPlayer<tsorcRevampPlayer>().TerraFallStacks * 12f;
            player.GetAttackSpeed(DamageClass.Summon) += AttackSpeed / 100f;
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>()] == 0)
                {
                    Projectile.NewProjectileDirect(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>(), WhipDamage, 1f, Main.myPlayer);
                }
            }
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = $"+{AttackSpeed}% summon attack speed\nTerra Energy fights for you";
        }
    }
}
