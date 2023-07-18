using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class TibianMage : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tibian Mage");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 450; // keep this low for melee
            NPCID.Sets.AttackType[NPC.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            NPCID.Sets.AttackTime[NPC.type] = 25;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public static List<string> Names = new List<string> 
        {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name3"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name4"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name5"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name6"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name7"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name8"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Name9")
        };

        public override List<string> SetNPCNameList() {
            return Names;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 50;
            NPC.defense = 25;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f;
            AnimationType = NPCID.Guide;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote6"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote7"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote8"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote9"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianMage.Quote10"));
            return chat;
        }

        int weaponChoice;
        public override void AI()
        {
            if (Main.rand.NextBool(40) && NPC.frame.Y < 1170)
            // only change weapons if we're not in attack animation frames
            // 1170 is before the start of the first attack frame but after the start of the previous animation frame
            // in other words: arbitrary, but close enough
            {
                weaponChoice = Main.rand.Next(0, 10);
                //Main.NewText(weaponChoice);
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
                return;
            }
        }

        public override void AddShops() {
            NPCShop shop = new(NPC.type);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.WandOfDarkness>()) {
                shopCustomPrice = 80,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.FarronDart>()) {
                shopCustomPrice = 80,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.GlintstonePebble>()) {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.GelCrate>()) {
                shopCustomPrice = 8,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.SpellTome) {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.MagicMirror) {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.WormholePotion) {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Magic.RedClothHat>()) {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Magic.RedClothTunic>()) {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Magic.RedClothPants>()) {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.WandOfFire>()) {
                shopCustomPrice = 550,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEyeOfCthulhu);

            shop.Add(new Item(ItemID.PiggyBank) {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEyeOfCthulhu);
            


            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.FlameStrikeScroll>()) {
                shopCustomPrice = 2000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.EnergyStrikeScroll>()) {
                shopCustomPrice = 2000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.DeathStrikeScroll>()) {
                shopCustomPrice = 3000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.ManaBomb>()) {
                shopCustomPrice = 300,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);
            


            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.GreatEnergyBeamScroll>()) {
                shopCustomPrice = 3500,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>()))));



            shop.Add(new Item(ModContent.ItemType<ForgottenIceBowScroll>()) {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.SuperHardMode));

            shop.Add(new Item(ModContent.ItemType<ForgottenThunderBowScroll>()) {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.SuperHardMode));



            shop.Register();
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (base.NPC.life <= 0) //even though npcs are immortal
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 1").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 3").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 3").Type);
                }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            if (Main.hardMode)
            {
                if (weaponChoice < 6)
                {
                    damage = 50;
                    knockback = 5f;
                }
                if (weaponChoice >= 6)
                {
                    damage = 20;
                    knockback = 6f;
                }
            }
            else
            {
                if (weaponChoice < 8) //More likely to use Great Soul Arrow Staff
                {
                    damage = 25;
                    knockback = 4f;
                }
                if (weaponChoice >= 8)
                {
                    damage = 10;
                    knockback = 5f;
                }
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }
        public override void DrawTownAttackGun(ref Texture2D item, ref Rectangle itemFrame, ref float scale, ref int horizontalHoldoutOffset)/* tModPorter Note: closeness is now horizontalHoldoutOffset, use 'horizontalHoldoutOffset = Main.DrawPlayerItemPos(1f, itemtype) - originalClosenessValue' to adjust to the change. See docs for how to use hook with an item type. */
        {
            if (weaponChoice < 8)
            {
                //item = ModContent.ItemType<Items.Weapons.Magic.GreatSoulArrowStaff>();
                scale = 1f;
                horizontalHoldoutOffset = 4;
            }
            if (weaponChoice >= 8)
            {
                //item = ModContent.ItemType<Items.Weapons.Magic.TheBlackenedFlames>();
                scale = 1f;
                horizontalHoldoutOffset = 4;
            }
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            if (weaponChoice < 8)
            {
                projType = ModContent.ProjectileType<Projectiles.GreatSoulArrow>();
                attackDelay = 8;
            }
            if (weaponChoice >= 8)
            {
                projType = ModContent.ProjectileType<Projectiles.BlackFire>();
                attackDelay = 8;
            }
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            if (weaponChoice < 8)
            {
                multiplier = 10f;
                randomOffset = 0f;
            }
            if (weaponChoice >= 8)
            {
                multiplier = 10f;
                randomOffset = 2f;
                gravityCorrection = 25;
            }

        }

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statManaMax2 > 21) //was 1, but these days mana starts at 20 on fresh char.
                {
                    return true;
                }
            }
            return false;
        }

        public override bool CanGoToStatue(bool toQueenStatue)
        {
            return true;
        }
    }
}
