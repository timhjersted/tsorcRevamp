using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items.Weapons.Melee.Axes;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class SolaireOfAstora : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Warrior of Sunlight");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 40;
            NPCID.Sets.AttackType[NPC.type] = 3;
            NPCID.Sets.AttackTime[NPC.type] = 18;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }
        public override List<string> SetNPCNameList()
        {
            return new List<string> { Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Name1") };
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 90;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.DyeTrader;
        }
        public override void AI()
        {
            if ((NPC.velocity.X == 0 && NPC.velocity.Y == 0) && Main.dayTime)
            {
                Lighting.AddLight(NPC.Center, .850f, .850f, .450f);
                if (Main.rand.NextBool(10))
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 57, NPC.velocity.X * 0f, -1f, 30, default(Color), 1.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Quote6"));
            if (tsorcRevampWorld.TheEnd)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.SolaireOfAstora.Praise"), 1.5);
            }
            return chat;
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

        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);
            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.CorruptedTooth>())
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.SunBlade>())
            {
                shopCustomPrice = 1000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId,
            }, Condition.Hardmode //cmonBruh
            );

            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.ThrowingAxeCrate>())
            {
                shopCustomPrice = 8,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Melee.AncientGoldenHelmet>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Melee.AncientGoldenArmor>())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Melee.AncientGoldenGreaves>())
            {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenIceRod>())
            {
                shopCustomPrice = 600,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEyeOfCthulhu);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenThunderRod>())
            {
                shopCustomPrice = 600,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEyeOfCthulhu);



            shop.Add(new Item(ItemID.LuckyHorseshoe)
            {
                shopCustomPrice = 230,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedSkeletron);

            shop.Add(new Item(ItemID.BladedGlove)
            {
                shopCustomPrice = 300,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedSkeletron);



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenStardustRod>())
            {
                shopCustomPrice = 8000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedDestroyer);



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenIceBrand>())
            {
                shopCustomPrice = ForgottenIceBrand.CoinPrice,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.TheSorrow>()))));


            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Axes.ForgottenPoisonAxe>())
            {
                shopCustomPrice = ForgottenPoisonAxe.CoinPrice,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenSwordbreaker>())
            {
                shopCustomPrice = ForgottenSwordbreaker.CoinPrice,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);


            shop.Register();
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (base.NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 1").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 3").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 3").Type);
                }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 30;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            item = (Texture2D)TextureAssets.Item[ModContent.ItemType<Items.Weapons.Melee.Broadswords.SunBlade>()];
            scale = .8f;
            itemSize = 36;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 36;
            itemHeight = 36;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statDefense > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}
