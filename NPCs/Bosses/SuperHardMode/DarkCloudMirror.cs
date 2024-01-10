using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class DarkCloudMirror : ModNPC
    {
        int antiMatDamage = 100;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 0;
            NPC.aiStyle = 0;
            NPC.height = 40;
            NPC.width = 20;
            Music = 12;
            NPC.damage = 53;
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

        //These describe projectiles the boss should shoot, and other things that should *not* be done for every multiplayer client
        #region Attacks
        void AntiMatAttack()
        {
            if (AttackModeCounter == 0)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AntimatTargeting>(), 0, 0.5f, Main.myPlayer, AttackModeLimit + 1, NPC.whoAmI);
            }
            if (AttackModeCounter == AttackModeLimit)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 7), ModContent.ProjectileType<DarkAntiMatRound>(), antiMatDamage / 2, 0.5f, Main.myPlayer);
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
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 17);
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
            float targetPoint = UsefulFunctions.Aim(NPC.Center, Target.Center, 1).ToRotation();
            if (!Main.gamePaused && (AttackModeCounter % 3 == 0))
            {
                Vector2 thisPos = NPC.Center + new Vector2(0, 128).RotatedBy(targetPoint - MathHelper.PiOver2) + Main.rand.NextVector2Circular(32, 32);
                Vector2 thisVel = UsefulFunctions.Aim(thisPos, NPC.Center + Main.rand.NextVector2Circular(10, 10), 8);
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
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkCloudMirror.Broadcast"), Color.Blue);
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;

            if (Main.rand.NextBool(4))
            {
                target.AddBuff(BuffID.BrokenArmor, 10 * 60 / expertScale, false);
                target.AddBuff(BuffID.Poisoned, 30 * 60 / expertScale, false);
                target.AddBuff(BuffID.Bleeding, 30 * 60 / expertScale, false);

            }
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 2 * 60 / expertScale, false);
                target.AddBuff(BuffID.OnFire, 3 * 60 / expertScale, false);
                target.AddBuff(ModContent.BuffType<FracturingArmor>(), 60 * 60, false); //defense goes time on every hit
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
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 2;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.DamageType == DamageClass.Melee)
            {
                modifiers.FinalDamage *= 2;
            }
        }

        #endregion
    }

}