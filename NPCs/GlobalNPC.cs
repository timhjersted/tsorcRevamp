using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.NPCs {
    public class tsorcRevampGlobalNPC : GlobalNPC {


        float enemyValue;
        float multiplier = 1f;
        float divisorMultiplier = 1f;
        int DarkSoulQuantity;

        public override bool InstancePerEntity => true;
        public bool DarkInferno = false;
        public bool CrimsonBurn = false;
        public bool ToxicCatDrain = false;
        public bool ResetToxicCatBlobs = false;
        public bool ViruCatDrain = false;
        public bool ResetViruCatBlobs = false;
        public bool BiohazardDrain = false;
        public bool ResetBiohazardBlobs = false;
        public bool ElectrocutedEffect = false;
        public bool PolarisElectrocutedEffect = false;
        public bool CrescentMoonlight = false;
        public bool Soulstruck = false;


        public override void ResetEffects(NPC npc) {
            DarkInferno = false;
            CrimsonBurn = false;
            ToxicCatDrain = false;
            ResetToxicCatBlobs = false;
            ViruCatDrain = false;
            ResetViruCatBlobs = false;
            BiohazardDrain = false;
            ResetBiohazardBlobs = false;
            ElectrocutedEffect = false;
            PolarisElectrocutedEffect = false;
            CrescentMoonlight = false;
            Soulstruck = false;
        }


        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) {
            if (tsorcRevampWorld.TheEnd) {
                pool.Clear(); //stop NPC spawns in The End 
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
            if (player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff) {
                maxSpawns = 0;
            }
        }

        //vanilla npc changes moved to separate file

        public override void NPCLoot(NPC npc) {


            #region Dark Souls & Consumable Souls Drops

                if (Soulstruck) {
                    divisorMultiplier = 0.9f; //10% increase
                }

                if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss) { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)

                if (npc.netID != NPCID.JungleSlime) {
                    if (Main.expertMode) { //npc.value is the amount of coins they drop
                        enemyValue = (int)npc.value / (divisorMultiplier*25); //all enemies drop more money in expert mode, so the divisor is larger to compensate
                    }
                    else {
                        enemyValue = (int)npc.value / (divisorMultiplier*10);
                    }
                }

                if (npc.netID == NPCID.JungleSlime) //jungle slimes drop 10 souls
                {
                    if (Main.expertMode) {
                        enemyValue = (int)npc.value / (divisorMultiplier*125);
                    }
                    else {
                        enemyValue = (int)npc.value / (divisorMultiplier*50);
                    }
                }


                multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(Main.LocalPlayer);

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                #region Bosses drop souls once
                if (npc.boss) {
                    if (npc.type == NPCID.MoonLordCore) { //moon lord does not drop coins in 1.3, so his value is 0, but in 1.4 he has a value of 1 plat
                        DarkSoulQuantity = 100000; //1 plat / 10
                    }
                    if (tsorcRevampWorld.Slain.ContainsKey(npc.type)) {
                        DarkSoulQuantity = 0;
                        return;
                    }
                    else {
                        if (Main.netMode == NetmodeID.SinglePlayer) {
                            Main.NewText("The souls of " + npc.GivenOrTypeName + " have been released!", 175, 255, 75);
                        }
                        else if (Main.netMode == NetmodeID.Server) {
                            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The souls of " + npc.GivenOrTypeName + " have been released!"), new Color(175, 255, 75));
                        }
                        tsorcRevampWorld.Slain.Add(npc.type, 0);
                        if (Main.expertMode) {
                            DarkSoulQuantity = 0;
                        }
                    }
                }
                #endregion

                #region EoW drops souls in a unique way
                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)))
                {

                    DarkSoulQuantity = 110;

                    if (Main.expertMode)
                    {
                        //EoW has 5 more segments in Expert mode, so its drops per segment is reduced slightly to keep it consistent. 
                        DarkSoulQuantity = 102;
                    }
                    if (NPC.downedBoss2)
                    {
                        //EoW still drops this many souls per segment even after the first kill. The difference between normal and expert is small enough it would get rounded away at this point.
                        DarkSoulQuantity = 10;
                    }

                    Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), DarkSoulQuantity);
                    DarkSoulQuantity = 0;
                }
                #endregion

                if (DarkSoulQuantity > 0) {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f;

                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) == false && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                    if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                        chance = 0.015f;
                    }

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 15) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 55) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((enemyValue >= 150) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
            #endregion

        }

            

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>().ConditionOverload) {
                int buffIndex = 0;
                foreach (int buffType in npc.buffType) {

                    if (Main.debuff[buffType]) {
                        damage = (int)(damage * 1.2f); // Is it meant to be stronger with projectiles? -C
                        //nope!
                    }
                    buffIndex++;
                }
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit) {
            if (player.GetModPlayer<tsorcRevampPlayer>().ConditionOverload) {
                int buffIndex = 0;
                foreach (int buffType in npc.buffType) {

                    if (Main.debuff[buffType]) {
                        damage = (int)(damage * 1.2f);
                    }
                    buffIndex++;
                }
            }
        }

        public override bool PreNPCLoot(NPC npc) {
            if (npc.type == NPCID.ChaosElemental) {
                NPCLoader.blockLoot.Add(ItemID.RodofDiscord); //we dont want any sequence breaks, do we
            }
            if (npc.netID == NPCID.JungleSlime)

                NPCLoader.blockLoot.Add(ItemID.SlimeHook);
            return base.PreNPCLoot(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage) {
            if (DarkInferno) {

                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 16;
                if (damage < 2) {
                    damage = 2;
                }

                var N = npc;
                for (int j = 0; j < 6; j++) {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (CrimsonBurn) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 16;
                if (Main.hardMode) npc.lifeRegen -= 16;
                if (damage < 2) {
                    damage = 2;
                }

            }

            if (ToxicCatDrain) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }

                int ToxicCatShotCount = 0;

                for (int i = 0; i < 1000; i++) {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI) {
                        ToxicCatShotCount++;
                    }
                }
                if (ToxicCatShotCount >= 4) { //this is to make it worth the players time stickying more than 3 times
                    npc.lifeRegen -= ToxicCatShotCount * 2 * 2; //Use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ToxicCatShotCount * 1) {
                        damage = ToxicCatShotCount * 1;
                    }
                }
                else {
                    npc.lifeRegen -= ToxicCatShotCount * 1 * 3; 
                    if (damage < ToxicCatShotCount * 1) {
                        damage = ToxicCatShotCount * 1;
                    }
                }
            }

            if (ViruCatDrain) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }

                int ViruCatShotCount = 0;

                for (int i = 0; i < 1000; i++) {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI) {
                        ViruCatShotCount++;
                    }
                }
                if (ViruCatShotCount >= 4)  {
                    npc.lifeRegen -= ViruCatShotCount * 4 * 2; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ViruCatShotCount * 1) {
                        damage = ViruCatShotCount * 1;
                    }
                }
                else {
                    npc.lifeRegen -= ViruCatShotCount * 2 * 3;
                    if (damage < ViruCatShotCount * 1) {
                        damage = ViruCatShotCount * 1;
                    }
                }
            }

            if (BiohazardDrain) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }

                int BiohazardShotCount = 0;

                for (int i = 0; i < 1000; i++) {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI) {
                        BiohazardShotCount++;
                    }
                }
                if (BiohazardShotCount >= 4) {
                    npc.lifeRegen -= BiohazardShotCount * 8 * 2; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < BiohazardShotCount * 1) {
                        damage = BiohazardShotCount * 1;
                    }
                }
                else {
                    npc.lifeRegen -= BiohazardShotCount * 4 * 3;
                    if (damage < BiohazardShotCount * 1) {
                        damage = BiohazardShotCount * 1;
                    }
                }
            }

            if (ElectrocutedEffect) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 22;
                if (damage < 2) {
                    damage = 2;
                }
            }

            if (PolarisElectrocutedEffect) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 100;
                if (damage < 10) {
                    damage = 10;
                }
            }

            if (CrescentMoonlight) {
                if (!Main.hardMode) {
                    if (npc.lifeRegen > 0) {
                        npc.lifeRegen = 0;
                    }
                    npc.lifeRegen -= 14;
                    if (damage < 2) {
                        damage = 2;
                    }
                }
                else { //double the DoT in HM
                    if (npc.lifeRegen > 0) {
                        npc.lifeRegen = 0;
                    }
                    npc.lifeRegen -= 28;
                    if (damage < 2) {
                        damage = 2;
                    }
                }
            }
        }

        public override void SetupShop(int type, Chest shop, ref int nextSlot) {
            if (type == NPCID.Merchant && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                shop.item[nextSlot].SetDefaults(ItemID.Bottle); //despite being able to find the archeologist right after (who sells bottled water), it's nice to have
                nextSlot++;
            }
            if (type == NPCID.SkeletonMerchant && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Firebomb>());
                shop.item[nextSlot].shopCustomPrice = 25;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;

                if (Main.rand.Next(2) == 0)
                {
                    shop.item[nextSlot].SetDefaults(ModContent.ItemType<EternalCrystal>());
                    shop.item[nextSlot].shopCustomPrice = 5000;
                    shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                    nextSlot++;
                }
            }
            if (type == NPCID.GoblinTinkerer && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Pulsar>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;

                shop.item[nextSlot].SetDefaults(ModContent.ItemType<ToxicCatalyzer>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (type == NPCID.Dryad && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<BloodredMossClump>());
                shop.item[nextSlot].shopCustomPrice = 25;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit) {
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain && (projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatExplosion>())) {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetToxicCatBlobs = true;
                int tags;

                for (int i = 0; i < 1000; i++) {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI) {
                        for (int q = 0; q < 1000; q++) {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI) {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item74.Style, volume, -pitch);

                        p.timeLeft = 2;

                        Projectile.NewProjectile(p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.ToxicCatExplosion>(), (int)(projectile.damage * 1.8f), projectile.knockBack, projectile.owner, tags, 0);

                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ToxicCatDrain>());

                        if (buffindex != -1) {
                            npc.DelBuff(buffindex);
                        }
                    }
                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ViruCatDrain && (projectile.type == ModContent.ProjectileType<Projectiles.VirulentCatDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.VirulentCatExplosion>())) {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetViruCatBlobs = true;
                int tags;

                for (int i = 0; i < 1000; i++) {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI) {
                        for (int q = 0; q < 1000; q++) {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI) {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item74.Style, volume, -pitch);

                        //Main.NewText(pitch);
                        p.timeLeft = 2;

                        Projectile.NewProjectile(p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.VirulentCatExplosion>(), (projectile.damage * 2), projectile.knockBack, projectile.owner, tags, 0);

                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ViruCatDrain>());

                        if (buffindex != -1) {
                            npc.DelBuff(buffindex);
                        }
                    }
                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain && (projectile.type == ModContent.ProjectileType<Projectiles.BiohazardDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.BiohazardExplosion>())) {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetBiohazardBlobs = true;
                int tags;

                for (int i = 0; i < 1000; i++) {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI) {
                        for (int q = 0; q < 1000; q++) {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI) {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item74.Style, volume, -pitch);

                        p.timeLeft = 2;

                        Projectile.NewProjectile(p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.BiohazardExplosion>(), (projectile.damage * 2), projectile.knockBack, projectile.owner, tags, 0);

                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.BiohazardDrain>());

                        if (buffindex != -1) {
                            npc.DelBuff(buffindex);
                        }
                    }
                }
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor) {
            if (ElectrocutedEffect) {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }

            if (PolarisElectrocutedEffect) {
                for (int i = 0; i < 2; i++) {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain) {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.Next(10) == 0) {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (ViruCatDrain) {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.Next(6) == 0) {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (BiohazardDrain) {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, -2f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CrescentMoonlight) {
                drawColor = Color.White;

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 164, npc.velocity.X * 0f, 0f, 100, default(Color), 1f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = false;
                Main.dust[dust].velocity += npc.velocity;
            }

            if (Soulstruck) {
                Lighting.AddLight(npc.Center, .4f, .4f, .850f);

                if (Main.rand.Next(6) == 0)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 68, 0, 0, 30, default(Color), 1.25f);
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                }
            }
        }
    }
}
