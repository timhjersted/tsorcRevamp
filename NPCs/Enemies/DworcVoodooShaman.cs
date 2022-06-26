using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcVoodooShaman : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.damage = 33;
            NPC.lifeMax = 1260;
            NPC.defense = 28;
            NPC.value = 6000;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.2f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodooShamanBanner>();

            AnimationType = NPCID.Skeleton;
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void OnKill()
        {
            if (Main.rand.Next(16) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BandOfCosmicPower>());
            if (Main.rand.NextFloat() >= .2f) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
            if (Main.rand.Next(16) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
            if (Main.rand.Next(3) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.FlaskofFire);
            if (Main.rand.Next(3) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
            if (Main.rand.Next(5) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
            if (Main.rand.Next(2) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, Main.rand.Next(1, 6));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion, Main.rand.Next(1, 4));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, Main.rand.Next(1, 5));
        }

        //Spawns in the Jungle and in the Cavern in HM.

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var player = spawnInfo.Player;
            bool TropicalOcean = player.position.X < 3600;

            float chance = 0f;

            if (Main.hardMode && (spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneJungle) && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight && Main.dayTime) return 0.02f;
                if (spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime) return 0.045f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.035f;
                if (spawnInfo.Player.ZoneRockLayerHeight) return 0.035f;
            }
            if (Main.hardMode && TropicalOcean && spawnInfo.Player.ZoneJungle) return 0.045f;

            return chance;
        }

        float poisonStrikeTimer = 0;
        float poisonStormTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.04f, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 3);

            bool clearLineofSight = Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1);
            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStrikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 18, 8, clearLineofSight, true, SoundID.Item20, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStormTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 25, 0, true, true, SoundID.Item100);//2,100 cursed firewall
            if (poisonStrikeTimer >= 60)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
            }
            if (poisonStormTimer >= 90)
            {
                UsefulFunctions.DustRing(NPC.Center, 180 - poisonStormTimer, DustID.CursedTorch, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity = Vector2.Zero;
                }
            }

            //Higher alpha = more invisible
            if (NPC.justHit)
            {
                NPC.alpha = 0;
            }
            if (Main.rand.Next(100) == 1)
            {
                NPC.alpha = 225;
                NPC.netUpdate = true;
            }
            if (Main.rand.Next(200) == 1)
            {
                NPC.alpha = 0; //0 is fully visible 225 is almost invisible
                NPC.netUpdate = true;
            }
            if (Main.rand.Next(250) == 1)
            {
                NPC.ai[3] = 1;
                NPC.life += 5;
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.ai[1] = 1f;
                NPC.netUpdate = true;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
            }
        }
    }
}
