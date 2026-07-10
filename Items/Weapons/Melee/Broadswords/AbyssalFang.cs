using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class AbyssalFang : ModItem
    {
        
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 48;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 23;
            Item.useTime = 23;
            Item.autoReuse = true;
            Item.damage = 470;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Melee;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.MediumPurple;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AbyssalSinking>(), 6 * 60, false);
            target.AddBuff(BuffID.Venom, 6 * 60, false);

            if (player.statLife < player.statLifeMax2 && Main.rand.NextBool(3))
            {
                int heal = System.Math.Min(4, player.statLifeMax2 - player.statLife);
                player.statLife += heal;
                player.HealEffect(heal);
            }
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.PurpleTorch, player.velocity.X * 0.2f, player.velocity.Y * 0.2f, 120, Color.MediumPurple, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}