using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee._Animations;
using tsorcRevamp.Content.Projectiles.Melee.Axes;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Axes
{
    public class GrandOolacileAxe : ModItem
    {
        public const int JudgmentComboHits = 3;
        public const int JudgmentDamageBonusPercent = 35;
        public const int WeaveManaCost = 30;
        public const int WeaveBoltCount = 3;
        private const int CatchSwingTicks = 28;

        private int comboStep;
        private int comboResetTimer;
        private bool isJudgmentSwing;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyOccultistAxe));

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            JudgmentComboHits,
            JudgmentDamageBonusPercent,
            WeaveManaCost,
            WeaveBoltCount
        );

        public static bool IsAxeOut(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<GrandOolacileThrownAxe>()] > 0;
        }

        public override void SetDefaults()
        {
            Item.damage = 320;
            Item.DamageType = DamageClass.Melee;
            Item.width = 112;
            Item.height = 104;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 10f;
            Item.crit = 6;
            Item.axe = 30;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<GrandOolacileAxeController>();
            Item.shootSpeed = 1f;

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = new Color(150, 20, 45);
        }

        public override bool AltFunctionUse(Player player)
        {
            return !IsAxeOut(player);
        }

        public override void HoldItem(Player player)
        {
            if (comboResetTimer > 0)
            {
                comboResetTimer--;
                if (comboResetTimer == 0)
                {
                    comboStep = 0;
                }
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (IsAxeOut(player))
            {
                return false;
            }

            if (player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<GrandOolacileAxeController>()] > 0)
                {
                    return false;
                }

                Item.noUseGraphic = true;
                Item.noMelee = true;
                return true;
            }

            Item.noUseGraphic = false;
            Item.noMelee = false;
            return true;
        }

        public override void UseAnimation(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                isJudgmentSwing = false;
                return;
            }

            // 3rd hit (comboStep == 2) is always an overhead Dark Judgment slam.
            isJudgmentSwing = comboStep == JudgmentComboHits - 1;

            if (Item.TryGetGlobalItem(out QuickSlashMeleeAnimation slashAnimation))
            {
                if (isJudgmentSwing)
                {
                    slashAnimation.IsAttackFlipped = false;
                }
                else
                {
                    slashAnimation.IsAttackFlipped = comboStep == 1;
                }
            }

            if (isJudgmentSwing)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Vector2 aimDirection = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(player.direction, 0f));
                    int waveDamage = (int)(player.GetWeaponDamage(Item) * 0.85f);
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.Center,
                        aimDirection,
                        ModContent.ProjectileType<GrandOolacileJudgmentWave>(),
                        waveDamage,
                        Item.knockBack * 1.2f,
                        player.whoAmI
                    );
                }
            }

            comboStep = (comboStep + 1) % JudgmentComboHits;
            comboResetTimer = 90;
        }

        // Called by GrandOolacileThrownAxe when its owner walks over it while holding the axe:
        // performs a quick damaging underhand catch swing (matching Owl's Greataxe).
        public void CatchThrownAxe(Player player)
        {
            isJudgmentSwing = false;
            Item.noUseGraphic = false;
            Item.noMelee = false;

            if (player.itemAnimation > 0)
            {
                return;
            }

            if (Item.TryGetGlobalItem(out QuickSlashMeleeAnimation slashAnimation))
            {
                slashAnimation.IsAttackFlipped = true;
            }

            if (Item.TryGetGlobalItem(out ItemMeleeAttackAiming catchAiming))
            {
                catchAiming.AttackDirection = new Vector2(player.direction, 0f);
            }

            player.lastVisualizedSelectedItem = Item.Clone();
            player.itemAnimationMax = CatchSwingTicks;
            player.itemAnimation = CatchSwingTicks;
            player.itemTimeMax = CatchSwingTicks;
            player.itemTime = CatchSwingTicks;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, player.Center);
            }

            if (player.whoAmI == Main.myPlayer)
            {
                int slashType = ModContent.ProjectileType<Projectiles.VFX.Slash>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile slash = Main.projectile[i];
                    if (slash.active && slash.owner == player.whoAmI && slash.type == slashType)
                    {
                        slash.Kill();
                    }
                }

                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    slashType,
                    0,
                    0f,
                    player.whoAmI
                );
            }
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (isJudgmentSwing)
            {
                modifiers.FinalDamage *= 1f + JudgmentDamageBonusPercent / 100f;
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 180);

            if (isJudgmentSwing)
            {
                target.AddBuff(ModContent.BuffType<DestinedDeath>(), 300);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                float effectiveUseTime = Item.useTime * CombinedHooks.TotalUseTimeMultiplier(player, Item);
                Projectile.NewProjectile(
                    source,
                    player.Center,
                    Vector2.Zero,
                    type,
                    damage,
                    knockback,
                    player.whoAmI,
                    effectiveUseTime
                );
            }

            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfOccultist>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 75000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
