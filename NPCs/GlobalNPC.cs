using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {

        public override bool InstancePerEntity => true;
        public bool DarkInferno = false;
        public bool CrimsonBurn = false;

        public override void ResetEffects(NPC npc) {
            DarkInferno = false;
            CrimsonBurn = false;
        }
        public override void SetDefaults(NPC npc) {
            if (npc.type == NPCID.TheHungryII) {
                npc.knockBackResist = 0f;
            }

            if (npc.type == NPCID.EyeofCthulhu && Main.player[Main.myPlayer].ZoneJungle) {
                if (Main.expertMode) {
                    npc.lifeMax = 3077; // Which is actually 4k hp in expert mode
                }
                npc.scale = 1.1f;
            }
        }

        public override void AI(NPC npc) {
            if (Main.LocalPlayer.HasBuff(ModContent.BuffType<Buffs.CrimsonDrain>()) && !npc.friendly && npc.lifeMax > 5) {
                Player player = Main.LocalPlayer;
                float distanceX = player.Center.X - npc.Center.X;
                float distanceY = player.Center.Y - npc.Center.Y;
                float distAbs = (float)Math.Sqrt(distanceX * distanceX + distanceY + distanceY);
                if (distAbs < 200f) {
                    npc.AddBuff(mod.BuffType("CrimsonBurn"), 3);
                }
            }

            if ((npc.friendly) && (npc.lifeMax == 250)) { //town NPCs are immortal
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>().ConditionOverload) {
                int buffIndex = 0;
                foreach (int buffType in npc.buffType) {

                    if (Main.debuff[buffType]) {
                        damage = (int)(damage * 1.3f);
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
            NPCLoader.blockLoot.Add(ItemID.SlimeHook);
            return base.PreNPCLoot(npc);
        }

        public override void NPCLoot(NPC npc) {

            if (npc.lifeMax > 5 && npc.value >= 10f) { //stop zero-value souls from dropping

                float enemyValue;

                if (Main.expertMode) { //npc.value is the amount of coins they drop
                    enemyValue = (int)npc.value / 25; //all enemies drop more money in expert mode, so the divisor is larger to compensate
                }
                else {
                    enemyValue = (int)npc.value / 10;
                }

                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                    enemyValue *= 1.25f;
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), (int)(enemyValue));

                // Consumable Soul drops ahead 
                // Jungle slime has enemyValue 50, therefore can drop any of the first 4 Consumable Souls, making them great for soul farming. ProudKnightSoul can be changed to (enemyValue > 50) if this is seen to be too OP.

                if ((enemyValue >= 1) && (enemyValue <= 50) && (Main.rand.NextFloat() < .015f)) // 1.5% chance of all enemies between enemyValue 1 and 50 dropping FadingSoul aka 1/75
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                }

                if ((enemyValue > 7) && (enemyValue <= 2000) && (Main.rand.NextFloat() < .02f)) // 2% chance of all enemies between enemyValue 8 and 2000 dropping LostUndeadSoul aka 1/50
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                }

                if ((enemyValue > 20) && (enemyValue <= 5000) && (Main.rand.NextFloat() < .02f)) // 2% chance of all enemies between enemyValue 21 and 2000 dropping NamelessSoldierSoul aka 1/50
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                }

                if ((enemyValue >= 50) && (enemyValue <= 5000) && (Main.rand.NextFloat() < .015f)) // 1.5% chance of all enemies between enemyValue 50 and 5000 dropping ProudKnightSoul aka 1/75
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                }
                //End consumable souls drops
            }

            if (npc.type == NPCID.GreenSlime) // No longer soulless, green snotties. Nvm, not working, idk why. Zeo?
            {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            }
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
                if (damage < 2) {
                    damage = 2;
                }

            }
        }
    }
}
