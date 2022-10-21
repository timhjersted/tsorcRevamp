
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbofSpirituality : ModItem
    {
        public static int useOrbofSpirituality = 0;
        public static bool OrbofSpiritualityProjExists = false;
        public float dashCD = 0f;
        public float dashTimer = 0f;
        public float invincibility = 0f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Spirituality");
            Tooltip.SetDefault("Throws a magic orb which will return to you after a certain distance" +
                "\nYou cannot throw more than one orb at a time" +
                "\nYou can recast with mana to force it to return early" +
                "\nThe orb deals more damage on the way back" +
                "\nEach hit gathers a stack of Essence Thief, crits gather 2" +
                "\nUpon reaching 9 stacks, the next cast will have 10% lifesteal" +
                "\nand spawn a homing flame on-hit(2 on-crit)" +
                "\nRight click to dash in mouse direction and spawn homing flames while dashing" +
                "\nThis has a 60 second cooldown and consumes double the mana");

        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.damage = 230;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.mana = 100;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 1f;
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft)
            {
                player.altFunctionUse = 2;
                Item.mana = 120;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.mana = 60;
            }
        }
        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 1)
            {
                if (useOrbofSpirituality == 0)
                {
                    useOrbofSpirituality = 1;
                }
                else
                if (useOrbofSpirituality == 1)
                {
                    useOrbofSpirituality = 2;
                }
            } else
            if (player.altFunctionUse == 2)
            {
                dashTimer = 0.2f;
                if (Main.GameUpdateCount % 1 == 0)
                {
                    dashCD = 60f;
                }
            }
            return true;
        }
        public override void HoldItem(Player player)
        {
            if (dashTimer > 0)
            {
                player.velocity = UsefulFunctions.GenerateTargetingVector(player.Center, Main.MouseWorld, 35f);
                invincibility = 0.5f;
                if (Main.GameUpdateCount % 1 == 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), player.Top, Vector2.One, ModContent.ProjectileType<OrbofSpiritualityFlame>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), Main.myPlayer);
                }
            }
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<OrbofSpiritualityProj>() && Main.projectile[i].owner == player.whoAmI)
                {
                    OrbofSpiritualityProjExists = true;
                    break;
                }
            }
            if (!OrbofSpiritualityProjExists)
            {
                var projectile = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbofSpiritualityProj>(), Item.damage, Item.knockBack, Main.myPlayer);
                projectile.originalDamage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
            }
        }
        public override void UpdateInventory(Player player)
        {
            if (invincibility > 0f)
            {
                player.immune = true;
            }
            if (Main.GameUpdateCount % 1 == 0)
            {
                dashCD -= 0.0167f;
                dashTimer -= 0.0167f;
                invincibility -= 0.0167f;
            }
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || dashCD <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }/*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<OrbofFlame>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/
    }
}