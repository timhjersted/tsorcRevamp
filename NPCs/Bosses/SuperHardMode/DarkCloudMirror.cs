using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class DarkCloudMirror : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 0;
            NPC.aiStyle = 0;
            NPC.height = 40;
            NPC.width = 20;
            Music = 12;
            NPC.damage = 105;
            NPC.defense = 160;
            NPC.lifeMax = 30000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 310;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }
        const float TRAIL_LENGTH = 12;

        int divineSparkDamage = 150;
        int darkFlowDamage = 100;
        int antiMatDamage = 200;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage /= 2;
            divineSparkDamage /= 2;
            darkFlowDamage /= 2;
            antiMatDamage /= 2;
        }

        public int MirrorAttackType
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float AttackModeCounter;
        public float AttackModeLimit
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public Player PlayerTarget
        {
            get => Main.player[(int)NPC.ai[2]];
        }

        //Depricated
        public Player Target
        {
            get => Main.player[NPC.target];
        }

        Vector2 nextWarpPoint = Vector2.Zero;
        public override void AI()
        {
            Lighting.AddLight(NPC.Center, Color.Blue.ToVector3() * 0.5f);
            UsefulFunctions.DustRing(NPC.Center, 32, DustID.ShadowbeamStaff);

            if (MirrorAttackType == DarkCloud.DarkCloudAttackID.AntiMat)
            {
                AntiMatMove();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AntiMatAttack();
                }
                if (AttackModeCounter == AttackModeLimit + 10)
                {
                    NPC.active = false;
                }
            }

            if (MirrorAttackType == DarkCloud.DarkCloudAttackID.TeleportingSlashes)
            {
                TeleportingSlashes();
            }
            AttackModeCounter++;

        }

        //These describe how the boss should move, and other things that should be done on every client to keep it deterministic
        #region Movements
        void AntiMatMove()
        {
            if (AttackModeCounter == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GenericLaser>(), 0, 0.5f, Main.myPlayer, (float)GenericLaser.GenericLaserID.AntiMatTargeting, NPC.whoAmI);
            }
            /*
            List<GenericLaser> laserList = GenericLaser.GetLasersByID(GenericLaser.GenericLaserID.AntiMatTargeting, NPC.whoAmI);
            if (laserList.Count > 0)
            {
                GenericLaser thisLaser = laserList[0];
                if (!thisLaser.initialized)
                {
                    thisLaser.LaserOrigin = NPC.Center;

                    thisLaser.TelegraphTime = 99999;
                    thisLaser.LaserLength = 4000;
                    thisLaser.LaserColor = Color.Red;
                    thisLaser.TargetingMode = 1;
                    thisLaser.lightColor = Color.OrangeRed;
                    thisLaser.TileCollide = false;
                    thisLaser.CastLight = false;
                    thisLaser.MaxCharge = 5;
                    thisLaser.FiringDuration = (int)AttackModeLimit + 1;
                    thisLaser.LaserVolume = 0;
                }

                Vector2 offset;
                offset = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 128).RotatedBy(MathHelper.ToRadians(90));
                offset *= ((300 - AttackModeCounter) / 300);
                offset = offset.RotatedBy(MathHelper.ToRadians(AttackModeCounter + (120)));
                thisLaser.LaserTarget = Target.Center + offset;
            }*/
        }
        #endregion

        //These describe projectiles the boss should shoot, and other things that should *not* be done for every multiplayer client
        #region Attacks
        void AntiMatAttack()
        {
            if (AttackModeCounter == AttackModeLimit)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 7), ModContent.ProjectileType<DarkAntiMatRound>(), antiMatDamage / 2, 0.5f, Main.myPlayer);
            }
        }

        void ThunderstormAttack()
        {

        }

        void TeleportingSlashes()
        {
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = 0;
            NPC.velocity.X *= 1.07f;
            if (Target.Center.X > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }
            if (AttackModeCounter == 0)
            {
                //nextWarpPoint = npc.Center;
                //npc.Center = new Vector2(10, 10);
            }

            if (AttackModeCounter < 80)
            {
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30, 60), DustID.ShadowbeamStaff, Main.rand.NextVector2CircularEdge(3, 3));
                }
            }
            else
            {
                NPC.velocity.Y += 0.09f;
            }

            if (AttackModeCounter == 80)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DarkUltimaWeapon>(), ai0: NPC.whoAmI, ai2: DarkCloud.DarkCloudAttackID.TeleportingSlashes);
                }
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 17);
            }

            if (AttackModeCounter == 240)
            {
                NPC.active = false;
            }
        }

        void BulletPortalsAttack()
        {

        }
        #endregion


        #region Teleport Functions
        //These functions make the boss move to various places (and mainly exist so I don't have to rewrite the same teleporting code 100 times...)
        void TeleportBehindPlayer()
        {
            DarkCloudParticleEffect(-2);
            NPC.Center = Main.player[NPC.target].Center;
            if (Main.player[NPC.target].direction == 1)
            {
                NPC.position.X -= 128;
            }
            else
            {
                NPC.position.X += 128;
            }
            DarkCloudParticleEffect(6);
        }

        void TeleportAroundPlayer(float radius = 192)
        {
            DarkCloudParticleEffect(-2);
            NPC.position = Main.player[NPC.target].position + Main.rand.NextVector2CircularEdge(radius, radius);
            DarkCloudParticleEffect(6);
        }

        void DashToAroundPlayer()
        {
            //TODO: Implement
        }

        void TeleportToArenaCenter()
        {
            DarkCloudParticleEffect(-2);
            NPC.Center = new Vector2(5827.5f, 1698) * 16;
            DarkCloudParticleEffect(6);
        }
        #endregion

        //The dust ring particle effect the boss uses
        void DarkCloudParticleEffect(float dustSpeed, float dustAmount = 50)
        {
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                Vector2 velocity = new Vector2(dustSpeed, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(NPC.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
            }
        }

        static Texture2D darkCloudTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkCloud");
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (darkCloudTexture == null || darkCloudTexture.IsDisposed)
            {
                darkCloudTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkCloud");
            }
            if (AttackModeCounter >= 80 || MirrorAttackType != DarkCloud.DarkCloudAttackID.TeleportingSlashes)
            {
                Rectangle sourceRectangle = new Rectangle(0, 0, darkCloudTexture.Width, darkCloudTexture.Height / Main.npcFrameCount[NPC.type]);
                Vector2 origin = sourceRectangle.Size() / 2f;
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (NPC.spriteDirection == 1)
                {
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }
                for (float i = TRAIL_LENGTH - 1; i >= 0; i--)
                {
                    Main.spriteBatch.Draw(darkCloudTexture, NPC.oldPos[(int)i] - Main.screenPosition + new Vector2(12, 16), sourceRectangle, drawColor * ((TRAIL_LENGTH - i) / TRAIL_LENGTH), NPC.rotation, origin, NPC.scale, spriteEffects, 0);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (MirrorAttackType == DarkCloud.DarkCloudAttackID.AntiMat)
            {
                AntiMatDraw(spriteBatch, drawColor);
            }
        }

        #region Draw Functions

        static Texture2D antimatTexture = (Texture2D)ModContent.Request<Texture2D>(ModContent.GetModItem(ModContent.ItemType<Items.Weapons.Ranged.Guns.AntimatRifle>()).Texture);
        public void AntiMatDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (antimatTexture == null || antimatTexture.IsDisposed)
            {
                antimatTexture = (Texture2D)ModContent.Request<Texture2D>(ModContent.GetModItem(ModContent.ItemType<Items.Weapons.Ranged.Guns.AntimatRifle>()).Texture);
            }
            float targetPoint = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 1).ToRotation();
            if (!Main.gamePaused && (AttackModeCounter % 3 == 0))
            {
                Vector2 thisPos = NPC.Center + new Vector2(0, 128).RotatedBy(targetPoint - MathHelper.PiOver2) + Main.rand.NextVector2Circular(32, 32);
                Vector2 thisVel = UsefulFunctions.GenerateTargetingVector(thisPos, NPC.Center + Main.rand.NextVector2Circular(10, 10), 8);
                Dust.NewDustPerfect(thisPos, DustID.FireworkFountain_Red, thisVel, 100, default, 0.5f).noGravity = true;
            }


            Rectangle sourceRectangle = new Rectangle(0, 0, antimatTexture.Width, antimatTexture.Height);
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);
            SpriteEffects theseEffects = (NPC.Center.X < Target.Center.X) ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Main.spriteBatch.Draw(antimatTexture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, targetPoint, origin, NPC.scale, theseEffects, 0f);
        }
        #endregion



        public override void OnKill()
        {
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 52, 0.3f, 0.3f, 200, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            UsefulFunctions.BroadcastText("Just a reflection...", Color.Blue);
        }

        #region Debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;

            if (Main.rand.NextBool(4))
            {
                player.AddBuff(BuffID.BrokenArmor, 600 / expertScale, false);
                player.AddBuff(BuffID.Poisoned, 1800 / expertScale, false);
                player.AddBuff(BuffID.Bleeding, 1800 / expertScale, false);

            }
            if (Main.rand.NextBool(2))
            {
                player.AddBuff(BuffID.BrokenArmor, 120 / expertScale, false); //broken armor
                player.AddBuff(BuffID.OnFire, 180 / expertScale, false); //on fire!
                player.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 3600, false); //defense goes time on every hit
            }
        }
        #endregion

        #region Vanilla overrides and misc
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        //Takes double damage from melee weapons
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            damage *= 2;
            crit = true;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.DamageType == DamageClass.Melee)
            {
                damage *= 2;
                crit = true;
            }
        }

        #endregion
    }

}