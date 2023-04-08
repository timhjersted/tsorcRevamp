using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Magic;
using Terraria.Audio;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    [Autoload(false)]
    public class OrbOfSpirituality : ModItem
    {
        public float DashingTimer = 0f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Spirituality");
            Tooltip.SetDefault("Throws a fiery Orb which will return to you after a certain distance" +
                "\nCasts homing flames while the Orb is sent out which restore half their mana cost on-hit" +
                "\nThe Orb deals more damage on the way back" +
                "\nGathers stacks of Essence Thief on Orb hits and enemy kills, doubled on crits" +
                "\nUpon reaching 9 stacks, the next Orb cast will consume all stacks, heal you and deal double damage" +
                "\nHeal scales based on maximum mana and magic damage" +
                "\nRight click to cast a charm which sunders enemies, increasing their vulnerability to magic damage" +
                "\nPress Special Ability to dash towards the mouse, casting homing flames along the way");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 220;
            Item.mana = 60;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Cyan;
            Item.shootSpeed = 20f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().OrbExists)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityFlame>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief >= 9)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityOrbFilled>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityCharm>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void HoldItem(Player player)
        {
            if (DashingTimer > 0)
            {
                SoundEngine.PlaySound(SoundID.Item104, player.Center);
                player.velocity = UsefulFunctions.GenerateTargetingVector(player.Center, Main.MouseWorld, 15f);
                Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.One, ModContent.ProjectileType<OrbOfSpiritualityFlameNoMana>(), Item.damage, Item.knockBack, Main.myPlayer);
                player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), 1 * 60);
                player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>(), 10 * 60);
            }
        }
        public override void UpdateInventory(Player player)
        {
            if (Main.GameUpdateCount % 1 == 0)
            {
                DashingTimer -= 0.0167f;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfFlame>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
