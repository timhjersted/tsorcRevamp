using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class YianBlade : ModItem
    {
        public int ManaRestoration = 20;
        public int BaseManaCost = 200;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ManaRestoration, BaseManaCost);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 84;
            Item.height = 90;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 33;
            Item.useTime = 33;
            Item.damage = 300;
            Item.knockBack = 5f;
            Item.scale = 1f;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 8.5f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.LightShard);
            recipe.AddIngredient(ItemID.DarkShard);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Confused, 240, false);

            player.ManaEffect(ManaRestoration);
            player.statMana += ManaRestoration;
            if (hit.Crit)
            {
                player.ManaEffect(ManaRestoration);
                player.statMana += ManaRestoration;
            }
        }
        private void ShootBlaze2(Player player, Vector2 velocity, int damage, float knockback)
        {
            Projectile Blaze2 = Projectile.NewProjectileDirect(
                Item.GetSource_FromThis(),
                player.Center,
                velocity,
                ModContent.ProjectileType<YianBlaze2>(),
                damage,
                knockback,
                player.whoAmI
            );

            Blaze2.DamageType = DamageClass.Melee;
            Blaze2.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + Item.crit + 100;
            Blaze2.damage = (int)(Blaze2.damage * (1f + player.GetTotalCritChance(DamageClass.Melee) / 100f));
            Blaze2.netUpdate = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Vector2 center = velocity;
                Vector2 up = velocity.RotatedBy(MathHelper.ToRadians(10));
                Vector2 down = velocity.RotatedBy(MathHelper.ToRadians(-10));

                ShootBlaze2(player, center, damage, knockback * 2);
                ShootBlaze2(player, up, damage, knockback * 2);
                ShootBlaze2(player, down, damage, knockback * 2);

                player.statMana -= player.statMana;
                player.manaRegenDelay = MeleeEdits.ManaDelay;
            }
            else
            {
                Projectile Blaze1 = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, velocity, ModContent.ProjectileType<YianBlaze>(), damage / 3, knockback, Main.myPlayer);
                Blaze1.DamageType = DamageClass.Melee;
                Blaze1.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + Item.crit + 100;
                Blaze1.netUpdate = true;
                Item.shootSpeed = 5f;
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Dynamic", Language.GetTextValue(Tooltip.Key + "2", BaseManaCost * Main.LocalPlayer.manaCost)));
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            if (player.statMana >= player.manaCost * BaseManaCost)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
                instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkGray; 
            }
            else
            {
                Item.UseSound = SoundID.Item20;
                tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
                instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.White; 
                
            }
            return base.CanUseItem(player);
        }
        public override void HoldItem(Player player)
        {
            player.manaRegenDelayBonus -= 0.5f / 2;//actually -50%, don't know why vanilla using /2 for regen
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 91, (player.velocity.X * 0.2f) + (player.direction * 2), player.velocity.Y * 0.2f, 50, color, 1f);
            Main.dust[dust].noGravity = true;
        }

    }
}
