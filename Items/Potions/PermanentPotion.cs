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
        public static readonly List<int> ExclusiveSetCombat = new List<int> {
            new PermanentArmorDrug().PermanentID,
            new PermanentDemonDrug().PermanentID,
            new PermanentStrengthPotion().PermanentID,
            new PermanentBattlefrontPotion().PermanentID
        };

        public static readonly List<int> ExclusiveSetWellFed = new List<int> {
            new PermanentTea().PermanentID,
            new PermanentSoup().PermanentID,
            new PermanentGoldenDelight().PermanentID,
        };
        public abstract int PermanentID
        {
            get;
        }

        public abstract int BuffType {
            get;
        }

        public virtual List<int> ExclusivePermanents {
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
            int ttindex = tooltips.FindLastIndex(t => t.Mod != null);
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

            if (ExclusivePermanents != null) {
                foreach (int checkID in ExclusivePermanents) {
                    //dont disable self
                    if (checkID == PermanentID)
                        continue;
                    //toggle off
                    modPlayer.PermanentBuffToggles[checkID] = false;
                }
            }
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[PermanentID])
            {
                Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.PermanentBuffToggles[PermanentID] && !modPlayer.ActivePermanentPotions.Contains(PermanentID))
            {
                modPlayer.ActivePermanentPotions.Add(PermanentID);
                PotionEffect(player);
                MakeImmuneToBuff(player);
            }

        }

        public abstract void PotionEffect(Player player);

        private void MakeImmuneToBuff(Player player) {
            player.buffImmune[BuffType] = true;
        }
    }
    public class PermanentObsidianSkinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_288";
        public override int PermanentID => 0;
        public override int BuffType => BuffID.ObsidianSkin;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Obsidian Skin buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
        }
    }

    public class PermanentRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_289";
        public override int PermanentID => 1;
        public override int BuffType => BuffID.Regeneration;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Regeneration buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeRegen += 4;
        }
    }

    public class PermanentSwiftnessPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_290";
        public override int PermanentID => 2;
        public override int BuffType => BuffID.Swiftness;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Swiftness buff.");
        }
        public override void PotionEffect(Player player)
        {
            player.moveSpeed += 0.25f;
        }
    }
    public class PermanentGillsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_291";
        public override int PermanentID => 3;
        public override int BuffType => BuffID.Gills;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Gills buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.gills = true;
        }
    }
    public class PermanentIronskinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_292";
        public override int PermanentID => 4;
        public override int BuffType => BuffID.Ironskin;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Ironskin buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += 8;
        }
    }
    public class PermanentManaRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_293";
        public override int PermanentID => 5;
        public override int BuffType => BuffID.ManaRegeneration;

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
        }
    }
    public class PermanentMagicPowerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_294";
        public override int PermanentID => 6;
        public override int BuffType => BuffID.MagicPower;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Magic Power buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.2f;
        }
    }
    public class PermanentFeatherfallPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_295";
        public override int PermanentID => 7;
        public override int BuffType => BuffID.Featherfall;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Featherfall buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.slowFall = true;
        }
    }
    public class PermanentSpelunkerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_296";
        public override int PermanentID => 8;
        public override int BuffType => BuffID.Spelunker;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Spelunker buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.findTreasure = true;
        }
    }
    public class PermanentInvisibilityPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_297";
        public override int PermanentID => 9;
        public override int BuffType => BuffID.Invisibility;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Invisibility buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.invis = true;
        }
    }
    public class PermanentShinePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_298";
        public override int PermanentID => 10;
        public override int BuffType => BuffID.Shine;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Shine buff.");
        }

        public override void UpdateInventory(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10] && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
            }
        }

        public override void PotionEffect(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
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
        public override string Texture => "Terraria/Images/Item_299";
        public override int PermanentID => 11;
        public override int BuffType => BuffID.NightOwl;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Night Owl buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.nightVision = true;
        }
    }
    public class PermanentBattlePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_300";
        public override int PermanentID => 12;
        public override int BuffType => BuffID.Battle;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Battle buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.enemySpawns = true;
        }
    }
    public class PermanentThornsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_301";
        public override int PermanentID => 13;
        public override int BuffType => BuffID.Thorns;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Thorns buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.thorns += 1f;
        }
    }

    public class PermanentWaterWalkingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_302";
        public override int PermanentID => 14;
        public override int BuffType => BuffID.WaterWalking;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Water Walking buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.waterWalk = true;
        }
    }

    public class PermanentArcheryPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_303";
        public override int PermanentID => 15;
        public override int BuffType => BuffID.Archery;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Archery buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.archery = true;
        }
    }
    public class PermanentHunterPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_304";
        public override int PermanentID => 16;
        public override int BuffType => BuffID.Hunter;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Hunter buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.detectCreature = true;
        }
    }
    public class PermanentGravitationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_305";
        public override int PermanentID => 17;
        public override int BuffType => BuffID.Gravitation;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Gravitation buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.gravControl = true;
        }
    }
    public class PermanentAle : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/Ale";
        public override int PermanentID => 18;
        public override int BuffType => BuffID.Tipsy;
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
        }
    }

    public class PermanentFlaskOfVenom : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1340";
        public override int PermanentID => 19;
        public override int BuffType => BuffID.WeaponImbueVenom;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Venom");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Venom buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 1;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }
    public class PermanentFlaskOfCursedFlames : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1353";
        public override int PermanentID => 20;
        public override int BuffType => BuffID.WeaponImbueCursedFlames;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Cursed Flames");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Cursed Flames buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 2;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfFire : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1354";
        public override int PermanentID => 21;
        public override int BuffType => BuffID.WeaponImbueFire;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Fire");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Fire buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 3;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfGold : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1355";
        public override int PermanentID => 22;
        public override int BuffType => BuffID.WeaponImbueGold;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Gold");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Gold buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 4;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfIchor : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1356";
        public override int PermanentID => 23;
        public override int BuffType => BuffID.WeaponImbueIchor;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Ichor");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Ichor buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 5;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfNanites : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1357";
        public override int PermanentID => 24;
        public override int BuffType => BuffID.WeaponImbueNanites;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Nanites");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Nanites buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 6;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfParty : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1358";
        public override int PermanentID => 25;
        public override int BuffType => BuffID.WeaponImbueConfetti;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Party");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Confetti buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 7;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentFlaskOfPoison : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1359";
        public override int PermanentID => 26;
        public override int BuffType => BuffID.WeaponImbuePoison;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Permanent Flask of Poison");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Poison buff. \nNot compatible with other weapon imbues.");
        }

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 8;
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
    }

    public class PermanentMiningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2322";
        public override int PermanentID => 27;
        public override int BuffType => BuffID.Mining;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Mining buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.pickSpeed -= 0.25f;
        }
    }

    public class PermanentHeartreachPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2323";
        public override int PermanentID => 28;
        public override int BuffType => BuffID.Heartreach;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Heartreach buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeMagnet = true;
        }
    }

    public class PermanentCalmingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2324";
        public override int PermanentID => 29;
        public override int BuffType => BuffID.Calm;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Calm buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.calmed = true;
        }
    }
    public class PermanentBuilderPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2325";
        public override int PermanentID => 30;
        public override int BuffType => BuffID.Builder;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Builder buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.tileSpeed += 0.25f;
            player.wallSpeed += 0.25f;
            player.blockRange++;
        }
    }
    public class PermanentTitanPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2326";
        public override int PermanentID => 31;
        public override int BuffType => BuffID.Titan;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Titan buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.kbBuff = true;
        }
    }

    public class PermanentFlipperPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2327";
        public override int PermanentID => 32;
        public override int BuffType => BuffID.Flipper;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Flipper buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.accFlipper = true;
            player.ignoreWater = true;
        }
    }

    public class PermanentSummoningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2328";
        public override int PermanentID => 33;
        public override int BuffType => BuffID.Summoning;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Summoning buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.maxMinions++;
        }
    }
    public class PermanentDangersensePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2329";
        public override int PermanentID => 34;
        public override int BuffType => BuffID.Dangersense;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Dangersense buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.dangerSense = true;
        }
    }

    public class PermanentAmmoReservationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2344";
        public override int PermanentID => 35;
        public override int BuffType => BuffID.AmmoReservation;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Ammo Reservation buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.ammoPotion = true;
        }
    }

    public class PermanentLifeforcePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2345";
        public override int PermanentID => 36;
        public override int BuffType => BuffID.Lifeforce;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Lifeforce buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.lifeForce = true;
            player.statLifeMax2 += player.statLifeMax / 5 / 20 * 20;
        }
    }

    public class PermanentEndurancePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2346";
        public override int PermanentID => 37;
        public override int BuffType => BuffID.Endurance;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Endurance buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.endurance += 0.1f;
        }
    }

    public class PermanentRagePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2347";
        public override int PermanentID => 38;
        public override int BuffType => BuffID.Rage;

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
        }
    }

    public class PermanentInfernoPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2348";
        public override int PermanentID => 39;
        public override int BuffType => BuffID.Inferno;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Inferno buff.");
        }

        public override void PotionEffect(Player player)
        {
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
        public override string Texture => "Terraria/Images/Item_2349";
        public override int PermanentID => 40;
        public override int BuffType => BuffID.Wrath;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Wrath buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.1f;
        }
    }

    public class PermanentFishingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2354";
        public override int PermanentID => 41;
        public override int BuffType => BuffID.Fishing;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Fishing buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.fishingSkill += 15;
        }
    }

    public class PermanentSonarPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2355";
        public override int PermanentID => 41;
        public override int BuffType => BuffID.Sonar;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Sonar buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.sonarPotion = true;
        }
    }

    public class PermanentCratePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2356";
        public override int PermanentID => 43;
        public override int BuffType => BuffID.Crate;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Crate buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.cratePotion = true;
        }
    }

    public class PermanentWarmthPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2359";
        public override int PermanentID => 44;
        public override int BuffType => BuffID.Warmth;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Warmth buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.resistCold = true;
        }
    }

    public class PermanentArmorDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ArmorDrugPotion";
        public override int PermanentID => 45;
        public override int BuffType => ModContent.BuffType<ArmorDrug>();
        public override List<int> ExclusivePermanents => ExclusiveSetCombat;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Armor Drug buff.");
        }


        public override void PotionEffect(Player player)
        {
            player.statDefense += 13;
        }
    }

    public class PermanentBattlefrontPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BattlefrontPotion";
        public override int PermanentID => 46;
        public override int BuffType => ModContent.BuffType<Battlefront>();
        public override List<int> ExclusivePermanents => ExclusiveSetCombat;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Battlefront buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += 17;
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetCritChance(DamageClass.Magic) += 6;
            player.GetCritChance(DamageClass.Melee) += 6;
            player.GetCritChance(DamageClass.Ranged) += 6;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.pickSpeed += 0.2f;
            player.thorns += 2f;
            player.enemySpawns = true;
        }
    }

    public class PermanentBoostPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BoostPotion";
        public override int PermanentID => 47;
        public override int BuffType => ModContent.BuffType<Boost>();

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Boost buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 5;
            player.GetCritChance(DamageClass.Melee) += 5;
            player.GetCritChance(DamageClass.Ranged) += 5;
        }
    }

    public class PermanentCrimsonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/CrimsonPotion";
        public override int PermanentID => 48;
        public override int BuffType => ModContent.BuffType<CrimsonDrain>();

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Crimson Drain buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
        }
    }

    public class PermanentDemonDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/DemonDrugPotion";
        public override int PermanentID => 49;
        public override int BuffType => ModContent.BuffType<DemonDrug>();

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Demon Drug buff.");
        }

        public override List<int> ExclusivePermanents => ExclusiveSetCombat;

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.2f;
        }
    }

    public class PermanentShockwavePotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ShockwavePotion";
        public override int PermanentID => 50;
        public override int BuffType => ModContent.BuffType<Shockwave>();

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Shockwave buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
        }
    }

    public class PermanentStrengthPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/StrengthPotion";
        public override int PermanentID => 51;
        public override int BuffType => ModContent.BuffType<Strength>();
        public override List<int> ExclusivePermanents => ExclusiveSetCombat;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently grants the Strength buff.");
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += 15;
            player.GetDamage(DamageClass.Generic) += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            player.pickSpeed += 0.15f;
            player.GetCritChance(DamageClass.Magic) += 2;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetCritChance(DamageClass.Ranged) += 2;
        }
    }

    public class PermanentSoulSiphonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/SoulSiphonPotion";
        public override int PermanentID => 52;
        public override int BuffType => ModContent.BuffType<SoulSiphon>();

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
        }
    }

    public class PermanentSoup : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/BowlOfSoup";
        public override int PermanentID => 53;
        public override int BuffType => BuffID.WellFed2;
        public override List<int> ExclusivePermanents => ExclusiveSetWellFed;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Never go hungry again.");
        }

        public override void PotionEffect(Player player)
        {
            player.wellFed = true;
            player.statDefense += 3;
            player.GetCritChance(DamageClass.Melee) += 3;
            player.GetCritChance(DamageClass.Ranged) += 3;
            player.GetCritChance(DamageClass.Magic) += 3;
            player.GetAttackSpeed(DamageClass.Melee) += 0.075f;
            player.GetDamage(DamageClass.Generic) += 0.075f;
            player.GetKnockback(DamageClass.Summon) += 0.75f;
            player.moveSpeed += 0.30f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.2f;
        }
    }
    public class PermanentTea : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/Teacup";
        public override int PermanentID => 54;
        public override int BuffType => BuffID.WellFed;
        public override List<int> ExclusivePermanents => ExclusiveSetWellFed;

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Tea time all the time.");
        }

        public override void PotionEffect(Player player) {
            player.wellFed = true;
            player.statDefense += 2;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetCritChance(DamageClass.Ranged) += 2;
            player.GetCritChance(DamageClass.Magic) += 2;
            player.GetAttackSpeed(DamageClass.Melee) += 0.05f;
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.GetKnockback(DamageClass.Summon) += 0.5f;
            player.moveSpeed += 0.20f;
            player.pickSpeed -= 0.1f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.1f;
        }
    }


    public class PermanentGoldenDelight : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/GoldenDelight";
        public override int PermanentID => 55;
        public override int BuffType => BuffID.WellFed;
        public override List<int> ExclusivePermanents => ExclusiveSetWellFed;

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("You could probably make a fortune by selling this...");
        }

        public override void PotionEffect(Player player) {
            player.wellFed = true;
            player.statDefense += 2;
            player.GetCritChance(DamageClass.Melee) += 4;
            player.GetCritChance(DamageClass.Ranged) += 4;
            player.GetCritChance(DamageClass.Magic) += 4;
            player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetKnockback(DamageClass.Summon) += 1f;
            player.moveSpeed += 0.40f;
            player.pickSpeed -= 0.15f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.3f;
        }
    }

}
