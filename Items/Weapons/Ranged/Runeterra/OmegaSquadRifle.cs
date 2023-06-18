using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Items.Materials;
using Terraria.Audio;
using Terraria.DataStructures;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class OmegaSquadRifle : ModItem
    {
        public int ShootSoundStyle = 0;
        public float ShootSoundVolume = 0.5f;
        public const int BaseDamage = 220;
        public const int ShroomCooldown = 5;
        public const int ShroomBonusCritChance = 100;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 22;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item99;
            Item.DamageType = DamageClass.Ranged; 
            Item.damage = BaseDamage;
            Item.knockBack = 6f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -8f);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed & player.altFunctionUse == 1)
            {
                type = ModContent.ProjectileType<RadioactiveDart>();
            }
            if (type == ProjectileID.PoisonDartBlowgun)
            {
                damage *= 4;
                damage /= 3;
            }
            if (player.altFunctionUse == 2)
            {
                if (type == ProjectileID.Seed)
                {
                    damage /= 2;
                }
                type = ModContent.ProjectileType<RadioactiveBlindingLaser>();
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                if (ShootSoundStyle == 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/Shot1") with { Volume = ShootSoundVolume }, player.Center);
                    ShootSoundStyle += 1;
                }
                else
                if (ShootSoundStyle == 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/Shot2") with { Volume = ShootSoundVolume }, player.Center);
                    ShootSoundStyle += 1;
                }
                else
                if (ShootSoundStyle == 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/Shot3") with { Volume = ShootSoundVolume }, player.Center);
                    ShootSoundStyle = 0;
                }
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/BlindingLaserShot") with { Volume = ShootSoundVolume * 2 }, player.Center);
            }
            return true;
        }
        public override void HoldItem(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()) && !player.HasBuff(ModContent.BuffType<ScoutsBoost2Omega>()))
            {
                player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 1); //ScoutsBoost buff itself does nto play any sounds in it's code so I didn't need to make an Omega version
            }
            if (player.itemAnimation > 14 && player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boots 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
            {
                player.velocity *= 0.92f;
            }
            else if (player.itemAnimation > 14 && player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()))
            {
                player.velocity *= 0.01f;
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<RadioactiveBlindingLaserCooldown>()))  //cooldown gets applied on projectile spawn
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
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<RadioactiveBlindingLaserCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<AlienGun>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}