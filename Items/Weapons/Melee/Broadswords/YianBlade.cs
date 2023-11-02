using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class YianBlade : ModItem
    {
        public int ManaRestoration = 100;
        public int BaseManaCost = 200;
        public int ProjectileDmgMult = 8;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ManaRestoration, BaseManaCost);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {

            Item.width = 44;
            Item.height = 44;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.scale = 1.2f;
            Item.damage = 666;
            Item.knockBack = 5f;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 5f;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkMagenta;

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Keybrand);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>());
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            player.ManaEffect(ManaRestoration);
            player.statMana += ManaRestoration;
            if (hit.Crit)
            {
                player.ManaEffect(ManaRestoration);
                player.statMana += ManaRestoration;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Projectile Blaze2 = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, velocity, ProjectileID.NebulaBlaze2, damage * ProjectileDmgMult, knockback * ProjectileDmgMult, Main.myPlayer);
                Blaze2.DamageType = DamageClass.Melee;
                Blaze2.CritChance = 100;
                Blaze2.damage = (int)(Blaze2.damage * (1f + player.GetTotalCritChance(DamageClass.Melee) / 100f));
                Blaze2.damage /= 2;
                Blaze2.netUpdate = true;

                player.statMana -= (int)(player.manaCost * BaseManaCost);
                player.manaRegenDelay = MeleeEdits.ManaDelay;
                player.altFunctionUse = 1;
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Dynamic", Language.GetTextValue(Tooltip.Key + "2", BaseManaCost * Main.LocalPlayer.manaCost, ProjectileDmgMult)));
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

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 2), player.velocity.Y * 0.2f, 100, color, 1f);
            Main.dust[dust].noGravity = false;
        }

    }
}
