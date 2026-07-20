using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Items.Armors
{
    public class LordGwynSetEffects : GlobalItem
    {
        public const float HelmMeleeDamage = 12f;
        public const float HelmMeleeCrit = 10f;
        public const int HelmMaxMana = 80;
        public const float ArmorGenericDamage = 10f;
        public const float ArmorEndurance = 10f;
        public const float ArmorManaCost = 12f;
        public const float LegsMoveSpeed = 12f;
        public const float LegsMeleeSpeed = 14f;
        public const float LegsMaxStamina = 15f;
        public const float HighManaDamage = 12f;
        public const float SetManaCost = 15f;
        public const float SetStaminaRegen = 20f;

        public override void SetDefaults(Item entity)
        {
            if (entity.type == ModContent.ItemType<LordGwynHelm>())
            {
                entity.defense = 24;
            }
            else if (entity.type == ModContent.ItemType<LordGwynArmor>())
            {
                entity.defense = 34;
            }
            else if (entity.type == ModContent.ItemType<LordGwynLeggings>())
            {
                entity.defense = 24;
            }
        }

        public override void UpdateEquip(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<LordGwynHelm>())
            {
                player.GetDamage(DamageClass.Melee) += HelmMeleeDamage / 100f;
                player.GetCritChance(DamageClass.Melee) += HelmMeleeCrit;
                player.statManaMax2 += HelmMaxMana;
            }
            else if (item.type == ModContent.ItemType<LordGwynArmor>())
            {
                player.GetDamage(DamageClass.Generic) += ArmorGenericDamage / 100f;
                player.endurance += ArmorEndurance / 100f;
                player.manaCost -= ArmorManaCost / 100f;
            }
            else if (item.type == ModContent.ItemType<LordGwynLeggings>())
            {
                player.moveSpeed += LegsMoveSpeed / 100f;
                player.GetAttackSpeed(DamageClass.Melee) += LegsMeleeSpeed / 100f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + LegsMaxStamina / 100f;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<LordGwynHelm>())
            {
                ReplaceTooltip(tooltips, $"Increases melee damage by {HelmMeleeDamage}%");
                tooltips.Add(new TooltipLine(Mod, "LordGwynCrit", $"Increases melee critical strike chance by {HelmMeleeCrit}%"));
                tooltips.Add(new TooltipLine(Mod, "LordGwynMana", $"Increases maximum mana by {HelmMaxMana}"));
            }
            else if (item.type == ModContent.ItemType<LordGwynArmor>())
            {
                ReplaceTooltip(tooltips, $"Increases all damage by {ArmorGenericDamage}%");
                tooltips.Add(new TooltipLine(Mod, "LordGwynEndurance", $"Reduces damage taken by {ArmorEndurance}%"));
                tooltips.Add(new TooltipLine(Mod, "LordGwynManaCost", $"Reduces mana costs by {ArmorManaCost}%"));
            }
            else if (item.type == ModContent.ItemType<LordGwynLeggings>())
            {
                ReplaceTooltip(tooltips, $"Increases movement speed by {LegsMoveSpeed}%");
                tooltips.Add(new TooltipLine(Mod, "LordGwynMeleeSpeed", $"Increases melee speed by {LegsMeleeSpeed}%"));
                tooltips.Add(new TooltipLine(Mod, "LordGwynStamina", $"Increases maximum stamina by {LegsMaxStamina}%"));
            }
        }

        static void ReplaceTooltip(List<TooltipLine> tooltips, string text)
        {
            int index = tooltips.FindIndex(line => line.Name == "Tooltip0");
            if (index >= 0)
            {
                tooltips[index].Text = text;
            }
            else
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "LordGwynTooltip", text));
            }
        }
    }

    public class LordGwynSetPlayer : ModPlayer
    {
        const int CinderStacksMax = 10;
        const int CinderStackCooldownTicks = 60;
        const int CinderManaRestore = 50;
        const int LastCinderCooldownTicks = 60 * 60 * 5;
        const int KindlingManaRestore = 30;
        const float KindlingStaminaRestore = 20f;

        public int cinderStacks;
        int cinderStackCooldown;
        int flameProjectileCooldown;
        int sunlightParryScanCooldown;
        int lastCinderCooldown;
        bool sunlightParryReady;
        bool hadSetLastTick;

        public bool HasLordGwynSet => Player.armor[0].type == ModContent.ItemType<LordGwynHelm>()
            && Player.armor[1].type == ModContent.ItemType<LordGwynArmor>()
            && Player.armor[2].type == ModContent.ItemType<LordGwynLeggings>();

        public bool AboveHalfMana => Player.statManaMax2 > 0 && Player.statMana >= Player.statManaMax2 * 0.5f;
        public bool AboveEightyMana => Player.statManaMax2 > 0 && Player.statMana >= Player.statManaMax2 * 0.8f;

        public override void PostUpdateEquips()
        {
            bool hasSet = HasLordGwynSet;
            if (!hasSet)
            {
                if (hadSetLastTick)
                {
                    cinderStacks = 0;
                    sunlightParryReady = false;
                }
                hadSetLastTick = false;
                return;
            }
            hadSetLastTick = true;

            if (cinderStackCooldown > 0)
            {
                cinderStackCooldown--;
            }
            if (flameProjectileCooldown > 0)
            {
                flameProjectileCooldown--;
            }
            if (sunlightParryScanCooldown > 0)
            {
                sunlightParryScanCooldown--;
            }
            if (lastCinderCooldown > 0)
            {
                lastCinderCooldown--;
            }

            Player.setBonus = $"Lord of Cinder: melee hits kindle Cinder; at {CinderStacksMax} stacks restore {CinderManaRestore} mana. Above 50% mana, gain flame power and empower the Sword of Lord Gwyn. Dodge through attacks to arm a sunlight strike. Burning kills restore mana and stamina. Above 50% mana, lethal damage triggers Last Cinder.";
            Player.manaCost -= LordGwynSetEffects.SetManaCost / 100f;
            Player.magmaStone = true;
            Player.meleeEnchant = 3;
            Player.fireWalk = true;
            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.OnFire3] = true;
            Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + LordGwynSetEffects.SetStaminaRegen / 100f;
            Player.armorEffectDrawShadow = true;

            if (AboveHalfMana)
            {
                Player.GetDamage(DamageClass.Melee) += LordGwynSetEffects.HighManaDamage / 100f;
                Player.GetDamage(DamageClass.Magic) += LordGwynSetEffects.HighManaDamage / 100f;
            }

            TryArmSunlightParry();
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (!HasLordGwynSet || lastCinderCooldown > 0 || !AboveHalfMana || pvp)
            {
                return true;
            }

            int manaCost = Player.statManaMax2 / 2;
            Player.statMana = System.Math.Max(0, Player.statMana - manaCost);
            Player.ManaEffect(manaCost);
            Player.statLife = 1;
            Player.SetImmuneTimeForAllTypes(180);
            lastCinderCooldown = LastCinderCooldownTicks;
            cinderStacks = 0;
            sunlightParryReady = false;
            playSound = false;
            genGore = false;

            if (Main.myPlayer == Player.whoAmI)
            {
                int damageToDeal = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(340);
                Projectile.NewProjectile(Player.GetSource_Misc("LordGwynLastCinder"), Player.Center, Vector2.Zero,
                    ModContent.ProjectileType<SwordOfLordGwynCinderNova>(), damageToDeal, 12f, Player.whoAmI, 430f);
            }
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.65f }, Player.Center);
            return false;
        }

        public void OnLordGwynMeleeHit(NPC target, int damageDone)
        {
            if (!HasLordGwynSet || target.friendly || target.dontTakeDamage)
            {
                return;
            }

            target.GetGlobalNPC<LordGwynSetNPC>().MarkLordGwynHit(Player.whoAmI);

            if (cinderStackCooldown <= 0)
            {
                cinderStackCooldown = CinderStackCooldownTicks;
                cinderStacks++;
                if (cinderStacks >= CinderStacksMax)
                {
                    cinderStacks = 0;
                    RestoreMana(CinderManaRestore);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.45f, Pitch = 0.35f }, Player.Center);
                }
            }

            if (AboveEightyMana && flameProjectileCooldown <= 0 && Main.myPlayer == Player.whoAmI)
            {
                flameProjectileCooldown = 20;
                Vector2 direction = (target.Center - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
                int flameDamage = System.Math.Max(1, damageDone / 4);
                Projectile.NewProjectile(Player.GetSource_Misc("LordGwynShortFlame"), Player.Center + direction * 28f, direction * 7.5f,
                    ModContent.ProjectileType<LordGwynShortFlame>(), flameDamage, 3f, Player.whoAmI);
            }

            if (sunlightParryReady && Main.myPlayer == Player.whoAmI)
            {
                sunlightParryReady = false;
                int strikeDamage = System.Math.Max(1, (int)(damageDone * 0.75f));
                Projectile.NewProjectile(Player.GetSource_Misc("LordGwynSunlightParry"), target.Center + new Vector2(0f, -100f), Vector2.Zero,
                    ModContent.ProjectileType<LordGwynSunlightStrike>(), strikeDamage, 6f, Player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.75f, Pitch = 0.25f }, target.Center);
            }
        }

        public void OnLordGwynBurningKill()
        {
            if (!HasLordGwynSet)
            {
                return;
            }
            RestoreMana(KindlingManaRestore);
            tsorcRevampStaminaPlayer stamina = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
            stamina.staminaResourceCurrent = MathHelper.Clamp(stamina.staminaResourceCurrent + KindlingStaminaRestore, 0f, stamina.staminaResourceMax2);
        }

        void RestoreMana(int amount)
        {
            int missingMana = Player.statManaMax2 - Player.statMana;
            int restored = System.Math.Min(amount, missingMana);
            if (restored <= 0)
            {
                return;
            }
            Player.statMana += restored;
            Player.ManaEffect(restored);
            Player.manaRegenDelay = 0;
        }

        void TryArmSunlightParry()
        {
            if (sunlightParryReady || sunlightParryScanCooldown > 0 || !Player.GetModPlayer<tsorcRevampPlayer>().isDodging)
            {
                return;
            }

            Rectangle paddedHitbox = Player.Hitbox;
            paddedHitbox.Inflate(18, 18);
            bool dodgedThreat = false;
            for (int i = 0; i < Main.maxNPCs && !dodgedThreat; i++)
            {
                NPC npc = Main.npc[i];
                dodgedThreat = npc.active && !npc.friendly && npc.damage > 0 && !npc.dontTakeDamage && paddedHitbox.Intersects(npc.Hitbox);
            }
            for (int i = 0; i < Main.maxProjectiles && !dodgedThreat; i++)
            {
                Projectile projectile = Main.projectile[i];
                dodgedThreat = projectile.active && projectile.hostile && projectile.damage > 0 && paddedHitbox.Intersects(projectile.Hitbox);
            }

            if (dodgedThreat)
            {
                sunlightParryReady = true;
                sunlightParryScanCooldown = 60;
                for (int i = 0; i < 16; i++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.GoldFlame, 0f, -1f, 80, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.4f }, Player.Center);
            }
        }
    }

    public class LordGwynSetNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        int lastLordGwynHitPlayer = -1;
        int lastLordGwynHitTimer;

        public void MarkLordGwynHit(int playerWhoAmI)
        {
            lastLordGwynHitPlayer = playerWhoAmI;
            lastLordGwynHitTimer = 600;
        }

        public override void PostAI(NPC npc)
        {
            if (lastLordGwynHitTimer > 0)
            {
                lastLordGwynHitTimer--;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (item.CountsAsClass(DamageClass.Melee))
            {
                player.GetModPlayer<LordGwynSetPlayer>().OnLordGwynMeleeHit(npc, damageDone);
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || projectile.hostile || !projectile.friendly)
            {
                return;
            }
            if (!projectile.DamageType.CountsAsClass(DamageClass.Melee) || IsLordGwynPassiveEffect(projectile.type))
            {
                return;
            }

            Main.player[projectile.owner].GetModPlayer<LordGwynSetPlayer>().OnLordGwynMeleeHit(npc, damageDone);
        }

        public override void OnKill(NPC npc)
        {
            if (lastLordGwynHitPlayer < 0 || lastLordGwynHitPlayer >= Main.maxPlayers || lastLordGwynHitTimer <= 0 || !IsBurning(npc))
            {
                return;
            }
            Player player = Main.player[lastLordGwynHitPlayer];
            if (player.active && !player.dead)
            {
                player.GetModPlayer<LordGwynSetPlayer>().OnLordGwynBurningKill();
            }
        }

        static bool IsLordGwynPassiveEffect(int projectileType)
        {
            return projectileType == ModContent.ProjectileType<LordGwynShortFlame>()
                || projectileType == ModContent.ProjectileType<LordGwynSunlightStrike>();
        }

        static bool IsBurning(NPC npc)
        {
            return npc.HasBuff(BuffID.OnFire)
                || npc.HasBuff(BuffID.OnFire3)
                || npc.HasBuff(BuffID.Daybreak)
                || npc.HasBuff(BuffID.CursedInferno)
                || npc.HasBuff(BuffID.ShadowFlame);
        }
    }
}