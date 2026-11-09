using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Enemy.QuaraHydromancerSet
{
    // Converted from the QuaraHydromancer NPC sheet; the Quara Hydromancer puppet wears this set too.
    // A Cobalt-tier melee/magic hybrid (22 defense total, between Cobalt's caster 21 and melee 32) that
    // teaches the Hydromancer's own gameplan: melee soaks the enemy, magic punishes the soaked enemy and
    // speeds up your next swing. Crafted from the guaranteed Hydromancer Soul drop.
    [AutoloadEquip(EquipType.Head)]
    public class QuaraHydromancerHeadpiece : ModItem
    {
        public const int MaxMana = 40;
        public const float CritChance = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, CritChance);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.defense = 5;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.GetCritChance(DamageClass.Melee) += CritChance;
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<QuaraHydromancerBreastplate>() && legs.type == ModContent.ItemType<QuaraHydromancerGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            int soakSeconds = QuaraHydromancerSetPlayer.SoakTicks / 60;
            int backwashSeconds = QuaraHydromancerSetPlayer.BackwashTicks / 60;
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", soakSeconds, QuaraHydromancerSetPlayer.SurgeDamage,
                QuaraHydromancerSetPlayer.SurgeMana, QuaraHydromancerSetPlayer.BackwashSpeed, backwashSeconds);
            player.GetModPlayer<QuaraHydromancerSetPlayer>().Tidecaller = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<HydromancerSoul>(), QuaraHydromancerSetPlayer.SoulsPerPiece);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), QuaraHydromancerSetPlayer.DarkSoulsPerPiece);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class QuaraHydromancerBreastplate : ModItem
    {
        public const float Damage = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Damage);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 20;
            Item.defense = 10;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += Damage / 100f;
            player.GetDamage(DamageClass.Magic) += Damage / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<HydromancerSoul>(), QuaraHydromancerSetPlayer.SoulsPerPiece);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), QuaraHydromancerSetPlayer.DarkSoulsPerPiece);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class QuaraHydromancerGreaves : ModItem
    {
        public const float MoveSpeed = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.accFlipper = true; //fish-man legs: swim like Flippers
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<HydromancerSoul>(), QuaraHydromancerSetPlayer.SoulsPerPiece);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), QuaraHydromancerSetPlayer.DarkSoulsPerPiece);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }

    // "Tidecaller" set bonus. Soak: melee hits make the enemy Wet. Surge: magic hits on a Wet enemy (or one
    // standing in water) hit harder and refund mana. Backwash: any magic hit makes the next melee swing faster.
    // Every hook runs on the attacking player's own client; NPC.AddBuff syncs the Wet debuff from there.
    public class QuaraHydromancerSetPlayer : ModPlayer
    {
        public const int SoulsPerPiece = 3;
        public const int DarkSoulsPerPiece = 5000;
        public const int SoakTicks = 240;
        public const float SurgeDamage = 15f;
        public const int SurgeMana = 5;
        public const int SurgeCooldownTicks = 30;
        public const float BackwashSpeed = 25f;
        public const int BackwashTicks = 180;

        public bool Tidecaller;
        int surgeCooldown;
        int backwashTimer;

        public override void ResetEffects()
        {
            Tidecaller = false;
        }

        public override void PostUpdate()
        {
            if (surgeCooldown > 0)
            {
                surgeCooldown--;
            }

            // Taking the armor off drops an armed Backwash.
            if (!Tidecaller)
            {
                backwashTimer = 0;
            }

            if (backwashTimer > 0)
            {
                backwashTimer--;

                // Armed cue: a few cyan water motes at the front hand, only on the owner's screen.
                bool showCue = !Main.dedServ && Player.whoAmI == Main.myPlayer && Main.rand.NextBool(3);
                if (showCue)
                {
                    Vector2 handPosition = Player.Center + new Vector2(Player.direction * 10f, 2f);
                    Dust mote = Dust.NewDustPerfect(handPosition + Main.rand.NextVector2Circular(6f, 6f), DustID.Water,
                        new Vector2(0f, -0.6f), 90, default, 0.9f);
                    mote.noGravity = true;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Tidecaller)
            {
                return;
            }

            bool magicHit = modifiers.DamageType.CountsAsClass(DamageClass.Magic);
            bool soaked = target.wet || target.HasBuff(BuffID.Wet);
            if (magicHit && soaked)
            {
                modifiers.SourceDamage *= 1f + SurgeDamage / 100f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Tidecaller)
            {
                return;
            }

            bool meleeHit = hit.DamageType.CountsAsClass(DamageClass.Melee);
            bool magicHit = hit.DamageType.CountsAsClass(DamageClass.Magic);
            if (meleeHit)
            {
                target.AddBuff(BuffID.Wet, SoakTicks);
            }

            if (!magicHit)
            {
                return;
            }

            backwashTimer = BackwashTicks;

            bool soaked = target.wet || target.HasBuff(BuffID.Wet);
            if (soaked && surgeCooldown <= 0)
            {
                surgeCooldown = SurgeCooldownTicks;
                int restored = Math.Min(SurgeMana, Player.statManaMax2 - Player.statMana);
                if (restored > 0)
                {
                    Player.statMana += restored;
                    Player.ManaEffect(restored);
                }
            }
        }

        // One boosted swing: the multiplier is read when a melee use starts, and PostItemCheck disarms
        // Backwash on that same start tick, so auto-reuse's next swing is back to normal speed.
        public override float UseSpeedMultiplier(Item item)
        {
            bool meleeItem = item.CountsAsClass(DamageClass.Melee);
            if (Tidecaller && backwashTimer > 0 && meleeItem)
            {
                return 1f + BackwashSpeed / 100f;
            }
            return 1f;
        }

        public override void PostItemCheck()
        {
            Item held = Player.HeldItem;
            bool meleeSwingStarted = Player.ItemAnimationJustStarted && held.damage > 0 && held.CountsAsClass(DamageClass.Melee);
            if (meleeSwingStarted)
            {
                backwashTimer = 0;
            }
        }
    }
}
