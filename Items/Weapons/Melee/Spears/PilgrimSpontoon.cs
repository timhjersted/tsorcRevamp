using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class PilgrimSpontoon : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<PilgrimSpontoonProj>();
        public override int Width => 52;
        public override int Height => 52;
        public override int BaseDmg => 35;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 3;
        public override int UseAnimationTime => 27;
        public override int UseTime => 27;
        public override int Rarity => ItemRarityID.Green;
        public const int ArmorPen = 10;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPen);

        private int HoldUpTimer = 0;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.useStyle = ItemUseStyleID.Shoot; 
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; 
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.altFunctionUse == 2) 
            {
                if (player.statMana < 60 || player.HasBuff(ModContent.BuffType<PilgrimSpontoonCooldown>()))
                {
                    return false; 
                }
                return true;
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2) 
            {
                player.statMana -= 80; 
                player.AddBuff(ModContent.BuffType<PilgrimSpontoonBuff>(), 380);
                player.AddBuff(ModContent.BuffType<PilgrimSpontoonCooldown>(), 1080); 

                SoundEngine.PlaySound(SoundID.Item25, player.position);

                return true;
            }
            return base.UseItem(player);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center + new Vector2(player.direction, 0), 
                    player.DirectionTo(Main.MouseWorld) * (player.HasBuff(ModContent.BuffType<PilgrimSpontoonBuff>()) ? 14 : 10), 
                    ModContent.ProjectileType<PilgrimArcaneBall>(),
                    BaseDmg,
                    BaseKnockback,
                    player.whoAmI
                );
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AncientDragonLance>());
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
