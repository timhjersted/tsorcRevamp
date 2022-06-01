using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Items.Potions.PermanentPotions
{
    //memory management is scary
    public abstract class PermanentPotion : ModItem
    {
        public abstract int PermanentID
        {
            get;
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 25;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item21;
            Item.rare = ItemRarityID.Orange;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindLastIndex(t => t.mod != null);
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "", "Does not consume a buff slot."));
                tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "", "Use to toggle effect."));
            }
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[PermanentID] = !modPlayer.PermanentBuffToggles[PermanentID];
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[PermanentID])
            {
                Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.PermanentBuffToggles[PermanentID] && !modPlayer.ActivePermanentPotions.Contains(PermanentID))
            {
                modPlayer.ActivePermanentPotions.Add(PermanentID);
                PotionEffect(player);
            }

        }

        public abstract void PotionEffect(Player player);
    }
    public class PermanentObsidianSkinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_288";
        public override int PermanentID => 0;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Obsidian Skin buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.ObsidianSkin] = true;
        }
    }

    public class PermanentRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_289";
        public override int PermanentID => 1;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Regeneration buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeRegen += 4;
            player.buffImmune[BuffID.Regeneration] = true;
        }
    }

    public class PermanentSwiftnessPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_290";
        public override int PermanentID => 2;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Swiftness buff.");
        }
        public override void PotionEffect(Player player)
        {
            player.moveSpeed += 0.25f;
            player.buffImmune[BuffID.Swiftness] = true;
        }
    }
    public class PermanentGillsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_291";
        public override int PermanentID => 3;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Gills buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.gills = true;
            player.buffImmune[BuffID.Gills] = true;
        }
    }
    public class PermanentIronskinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_292";
        public override int PermanentID => 4;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Ironskin buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += 8;
            player.buffImmune[BuffID.Ironskin] = true;
        }
    }
    public class PermanentManaRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_293";
        public override int PermanentID => 5;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Mana Regeneration buff.");
        }

        public override void PotionEffect(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().manaShield == 0)
            {
                player.manaRegenBuff = true;
            }
            player.buffImmune[BuffID.ManaRegeneration] = true;
        }
    }
    public class PermanentMagicPowerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_294";
        public override int PermanentID => 6;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Magic Power buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.2f;
            player.buffImmune[BuffID.MagicPower] = true;
        }
    }
    public class PermanentFeatherfallPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_295";
        public override int PermanentID => 7;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Featherfall buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.slowFall = true;
            player.buffImmune[BuffID.Featherfall] = true;
        }
    }
    public class PermanentSpelunkerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_296";
        public override int PermanentID => 8;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Spelunker buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.findTreasure = true;
            player.buffImmune[BuffID.Spelunker] = true;
        }
    }
    public class PermanentInvisibilityPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_297";
        public override int PermanentID => 9;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Invisibility buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.invis = true;
            player.buffImmune[BuffID.Invisibility] = true;
        }
    }
    public class PermanentShinePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_298";
        public override int PermanentID => 10;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Shine buff.");
        }

        public override void UpdateInventory(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10] && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
                player.buffImmune[BuffID.Shine] = true;
            }
        }

        public override void PotionEffect(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
                player.buffImmune[BuffID.Shine] = true;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "", "Has no effect on the [c/6d8827:Bearer of the Curse]"));
                }
            }
        }
    }
    public class PermanentNightOwlPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_299";
        public override int PermanentID => 11;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Night Owl buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.nightVision = true;
            player.buffImmune[BuffID.NightOwl] = true;
        }
    }
    public class PermanentBattlePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_300";
        public override int PermanentID => 12;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Battle buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.enemySpawns = true;
            player.buffImmune[BuffID.Battle] = true;
        }
    }
    public class PermanentThornsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_301";
        public override int PermanentID => 13;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Thorns buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.thorns += 1f;
            player.buffImmune[BuffID.Thorns] = true;
        }
    }

    public class PermanentWaterWalkingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_302";
        public override int PermanentID => 14;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Water Walking buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.waterWalk = true;
            player.buffImmune[BuffID.WaterWalking] = true;
        }
    }

    public class PermanentArcheryPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_303";
        public override int PermanentID => 15;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Archery buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.archery = true;
            player.buffImmune[BuffID.Archery] = true;
        }
    }
    public class PermanentHunterPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_304";
        public override int PermanentID => 16;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Hunter buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.detectCreature = true;
            player.buffImmune[BuffID.Hunter] = true;
        }
    }
    public class PermanentGravitationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_305";
        public override int PermanentID => 17;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Gravitation buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.gravControl = true;
            player.buffImmune[BuffID.Gravitation] = true;
        }
    }
    public class PermanentAle : PermanentPotion
    {
        public override string Texture => "Terraria/Item_353";
        public override int PermanentID => 18;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Ale buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense -= 4;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.Tipsy] = true;
        }
    }

    public class PermanentFlaskOfVenom : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1340";
        public override int PermanentID => 19;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Venom");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Venom buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 1;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueVenom] = true;
        }
    }
    public class PermanentFlaskOfCursedFlames : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1353";
        public override int PermanentID => 20;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Cursed Flames");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Cursed Flames buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 2;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueCursedFlames] = true;
        }
    }

    public class PermanentFlaskOfFire : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1354";
        public override int PermanentID => 21;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Fire");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Fire buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 3;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueFire] = true;
        }
    }

    public class PermanentFlaskOfGold : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1355";
        public override int PermanentID => 22;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Gold");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Gold buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 4;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueGold] = true;
        }
    }

    public class PermanentFlaskOfIchor : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1356";
        public override int PermanentID => 23;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Ichor");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Ichor buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 5;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueIchor] = true;
        }
    }

    public class PermanentFlaskOfNanites : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1357";
        public override int PermanentID => 24;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Nanites");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Nanites buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 6;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueNanites] = true;
        }
    }

    public class PermanentFlaskOfParty : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1358";
        public override int PermanentID => 25;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Party");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Confetti buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 7;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbueConfetti] = true;
        }
    }

    public class PermanentFlaskOfPoison : PermanentPotion
    {
        public override string Texture => "Terraria/Item_1359";
        public override int PermanentID => 26;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Poison");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Poison buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 8;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.buffImmune[BuffID.WeaponImbuePoison] = true;
        }
    }

    public class PermanentMiningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2322";
        public override int PermanentID => 27;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Mining buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.pickSpeed -= 0.25f;
            player.buffImmune[BuffID.Mining] = true;
        }
    }

    public class PermanentHeartreachPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2323";
        public override int PermanentID => 28;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Heartreach buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeMagnet = true;
            player.buffImmune[BuffID.Heartreach] = true;
        }
    }

    public class PermanentCalmingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2324";
        public override int PermanentID => 29;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Calm buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.calmed = true;
            player.buffImmune[BuffID.Calm] = true;
        }
    }
    public class PermanentBuilderPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2325";
        public override int PermanentID => 30;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Builder buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.tileSpeed += 0.25f;
            player.wallSpeed += 0.25f;
            player.blockRange++;
            player.buffImmune[BuffID.Builder] = true;
        }
    }
    public class PermanentTitanPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2326";
        public override int PermanentID => 31;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Titan buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.kbBuff = true;
            player.buffImmune[BuffID.Titan] = true;
        }
    }

    public class PermanentFlipperPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2327";
        public override int PermanentID => 32;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Flipper buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.accFlipper = true;
            player.ignoreWater = true;
            player.buffImmune[BuffID.Flipper] = true;
        }
    }

    public class PermanentSummoningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2328";
        public override int PermanentID => 33;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Summoning buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.maxMinions++;
            player.buffImmune[BuffID.Summoning] = true;
        }
    }
    public class PermanentDangersensePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2329";
        public override int PermanentID => 34;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Dangersense buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.dangerSense = true;
            player.buffImmune[BuffID.Dangersense] = true;
        }
    }

    public class PermanentAmmoReservationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2344";
        public override int PermanentID => 35;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Ammo Reservation buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.ammoPotion = true;
            player.buffImmune[BuffID.AmmoReservation] = true;
        }
    }

    public class PermanentLifeforcePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2345";
        public override int PermanentID => 36;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Lifeforce buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeForce = true;
            player.statLifeMax2 += player.statLifeMax / 5 / 20 * 20;
            player.buffImmune[BuffID.Lifeforce] = true;
        }
    }

    public class PermanentEndurancePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2346";
        public override int PermanentID => 37;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Endurance buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.endurance += 0.1f;
            player.buffImmune[BuffID.Endurance] = true;
        }
    }

    public class PermanentRagePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2347";
        public override int PermanentID => 38;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Rage buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 10;
            player.GetCritChance(DamageClass.Melee) += 10;
            player.GetCritChance(DamageClass.Ranged) += 10;
            player.GetCritChance(DamageClass.Throwing) += 10;
            player.buffImmune[BuffID.Rage] = true;
        }
    }

    public class PermanentInfernoPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2348";
        public override int PermanentID => 39;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Inferno buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.buffImmune[BuffID.Inferno] = true;
            player.inferno = true;
            Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
            int num = 24;
            float num12 = 200f;
            bool flag = player.infernoCounter % 60 == 0;
            int damage = 10;
            if (player.whoAmI == Main.myPlayer)
            {
                for (int l = 0; l < 200; l++)
                {
                    NPC nPC = Main.npc[l];
                    if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(player.Center, nPC.Center) <= num12)
                    {
                        if (nPC.FindBuffIndex(num) == -1)
                        {
                            nPC.AddBuff(num, 120);
                        }
                        if (flag)
                        {
                            player.ApplyDamageToNPC(nPC, damage, 0f, 0, crit: false);
                        }
                    }
                }
                if (Main.netMode != NetmodeID.SinglePlayer && player.hostile)
                {
                    for (int m = 0; m < 255; m++)
                    {
                        Player otherPlayer = Main.player[m];
                        if (otherPlayer != player && otherPlayer.active && !otherPlayer.dead && otherPlayer.hostile && !otherPlayer.buffImmune[24] && (otherPlayer.team != player.team || otherPlayer.team == 0) && Vector2.DistanceSquared(player.Center, otherPlayer.Center) <= num)
                        {
                            if (otherPlayer.FindBuffIndex(num) == -1)
                            {
                                otherPlayer.AddBuff(num, 120);
                            }
                            if (flag)
                            {
                                otherPlayer.Hurt(PlayerDeathReason.LegacyEmpty(), damage, 0, pvp: true);
                                if (Main.netMode != NetmodeID.SinglePlayer)
                                {
                                    PlayerDeathReason reason = PlayerDeathReason.ByPlayer(otherPlayer.whoAmI);
                                    NetMessage.SendPlayerHurt(m, reason, damage, 0, critical: false, pvp: true, 0);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class PermanentWrathPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2349";
        public override int PermanentID => 40;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Wrath buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.buffImmune[BuffID.Wrath] = true;
        }
    }

    public class PermanentFishingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2354";
        public override int PermanentID => 41;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Fishing buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.fishingSkill += 15;
            player.buffImmune[BuffID.Fishing] = true;
        }
    }

    public class PermanentSonarPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2355";
        public override int PermanentID => 41;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Sonar buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.sonarPotion = true;
            player.buffImmune[BuffID.Sonar] = true;
        }
    }

    public class PermanentCratePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2356";
        public override int PermanentID => 43;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Crate buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.cratePotion = true;
            player.buffImmune[BuffID.Crate] = true;
        }
    }

    public class PermanentWarmthPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Item_2359";
        public override int PermanentID => 44;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Warmth buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.resistCold = true;
            player.buffImmune[BuffID.Warmth] = true;
        }
    }

    public class PermanentArmorDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ArmorDrugPotion";
        public override int PermanentID => 45;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Armor Drug buff.");
        }


        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[45] = !modPlayer.PermanentBuffToggles[45]; //toggle
            return true;
        }

        public override void PotionEffect(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[46] = true;
            player.statDefense += 13;
            player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
            player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
            player.buffImmune[ModContent.BuffType<Strength>()] = true;
            player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
        }
    }

    public class PermanentBattlefrontPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BattlefrontPotion";
        public override int PermanentID => 46;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Battlefront buff.");
        }


        public override bool? UseItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[46] = !modPlayer.PermanentBuffToggles[46]; //toggle
            return true;
        }

        public override void PotionEffect(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            player.statDefense += 17;
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetCritChance(DamageClass.Magic) += 6;
            player.GetCritChance(DamageClass.Melee) += 6;
            player.GetCritChance(DamageClass.Ranged) += 6;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.pickSpeed += 0.2f;
            player.thorns += 2f;
            player.enemySpawns = true;
            player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
            player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
            player.buffImmune[ModContent.BuffType<Strength>()] = true;
            player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
        }
    }

    public class PermanentBoostPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BoostPotion";
        public override int PermanentID => 47;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Boost buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 5;
            player.GetCritChance(DamageClass.Melee) += 5;
            player.GetCritChance(DamageClass.Ranged) += 5;
            player.buffImmune[ModContent.BuffType<Boost>()] = true;
        }
    }

    public class PermanentCrimsonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/CrimsonPotion";
        public override int PermanentID => 48;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Crimson Drain buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
            player.buffImmune[ModContent.BuffType<CrimsonDrain>()] = true;
        }
    }

    public class PermanentDemonDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/DemonDrugPotion";
        public override int PermanentID => 49;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Demon Drug buff.");
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[49] = !modPlayer.PermanentBuffToggles[49]; //toggle
            return true;
        }

        public override void PotionEffect(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            player.GetDamage(DamageClass.Generic) += 0.2f;
            player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
            player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
            player.buffImmune[ModContent.BuffType<Strength>()] = true;
            player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
        }
    }

    public class PermanentShockwavePotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ShockwavePotion";
        public override int PermanentID => 50;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Shockwave buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
            player.buffImmune[ModContent.BuffType<Shockwave>()] = true;
        }
    }

    public class PermanentStrengthPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/StrengthPotion";
        public override int PermanentID => 51;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Strength buff.");
        }


        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[51] = !modPlayer.PermanentBuffToggles[51]; //toggle
            return true;
        }

        public override void PotionEffect(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            player.statDefense += 15;
            player.GetDamage(DamageClass.Generic) += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            player.pickSpeed += 0.15f;
            player.GetCritChance(DamageClass.Magic) += 2;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetCritChance(DamageClass.Ranged) += 2;
            player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
            player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
            player.buffImmune[ModContent.BuffType<Strength>()] = true;
            player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
        }
    }

    public class PermanentSoulSiphonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/SoulSiphonPotion";
        public override int PermanentID => 52;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Soul Siphon buff.");
            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
        }

        public override void PotionEffect(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SoulSiphon = true;
            modPlayer.SoulReaper += 5;
            modPlayer.ConsSoulChanceMult += 10;
            player.buffImmune[ModContent.BuffType<SoulSiphon>()] = true;
        }
    }

    public class PermanentSoup : PermanentPotion
    {
        public override string Texture => "Terraria/Item_357";
        public override int PermanentID => 53;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Never go hungry again.");
        }

        public override void PotionEffect(Player player)
        {
            player.wellFed = true;
            player.statDefense += 2;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetCritChance(DamageClass.Ranged) += 2;
            player.GetCritChance(DamageClass.Magic) += 2;
            player.GetAttackSpeed(DamageClass.Melee) += 0.05f;
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.minionKB += 0.5f;
            player.moveSpeed += 0.20f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.1f;
            player.buffImmune[BuffID.WellFed] = true;
        }
    }

}
