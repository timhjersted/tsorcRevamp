namespace tsorcRevamp.NPCs.Special
{
    /*
    [AutoloadBossHead]
    class MoonlitLeonhard : ModNPC
    {
        public override bool Autoload(ref string name) => false;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Moonlit Leonhard");
            Main.npcFrameCount[npc.type] = 7;
            NPCID.Sets.TrailCacheLength[npc.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[npc.type] = 0;
        }

        public override void SetDefaults()
        {
            //npc.aiStyle = 43;
            npc.npcSlots = 100;
            npc.aiStyle = -1;
            npc.damage = 10;
            npc.boss = true;
            npc.lifeMax = 5000;
            npc.scale = 1f;
            npc.defense = 10;
            npc.height = 80;
            npc.width = 34;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.knockBackResist = 0f;
            npc.HitSound = SoundID.NPCHit48;
            npc.DeathSound = SoundID.NPCDeath58;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Frostburn] = true;
            //npc.dontTakeDamage = true;
            npc.dontTakeDamageFromHostiles = true;
            despawnHandler = new NPCDespawnHandler("Leonhard fades into the moonlight...", Color.Teal, 79);

        }



        #region AI



        //Damage variables
        public static int SlashDamage = 10;
        public static int SlashBeamDamage = 10;
        public static int FirebombDamage = 15;
        public static int HomingSoulmassDamage = 10;
        public static int GunfireDamage = 10;


        //If this is set to anything but -1, the boss will *only* use that attack ID
        readonly int testAttack = -1;
        bool firstPhase = true;
        bool changingPhases = false;
        bool initializedMoves = false;

        //The next warp point in the current attack. It gets calculated before it's used so it has time to get synced first
        Vector2 nextWarpPoint;

        //The first warp point of the *next* attack. It is only used once per attack, at the start. Whenever it's used, a new one is calculated immediately to give it time to sync.
        Vector2 preSelectedWarpPoint;

        float phaseChangeCounter = 0;
        LeonhardMove CurrentMove;
        List<LeonhardMove> ActiveMoveList;
        List<LeonhardMove> DefaultList;

        public int NextAttackMode
        {
            get => (int)npc.ai[0];
            set => npc.ai[0] = value;
        }
        public float AttackModeCounter
        {
            get => npc.ai[1];
            set => npc.ai[1] = value;
        }
        public float NextConfinedBlastsAngle
        {
            get => npc.ai[2];
            set => npc.ai[2] = value;
        }
        public int AttackModeTally
        {
            get => (int)npc.ai[3];
            set => npc.ai[3] = value;
        }
        public Player Target
        {
            get => Main.player[npc.target];
        }

        NPCDespawnHandler despawnHandler;


        public override void AI()
        {
            if (!initializedMoves)
            {
                InitializeMoves();
                initializedMoves = true;
            }

            //Force an update once a second. Terraria gets a bit lazy about it, and this consistency is required to prevent rubberbanding on certain high-intensity attacks
            if (Main.time % 20 == 0)
            {
                npc.netUpdate = true;
            }

            //If it's not in the first phase, move according to the pattern of the current attack. If it's not a multiplayer client, then also run its attacks.
            //These are split up to keep the code readable.

            if (CurrentMove != null)
            {
                CurrentMove.Move();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CurrentMove.Attack();
                }
            }
            else
            {
                CurrentMove = new LeonhardMove(CloseSlashMove, CloseSlashAttack, LeonhardAttackID.CloseSlash, "Close Slash");
                CurrentMoveDebug();
            }
                AttackModeCounter++;

            if (AttackModeCounter == 5)
            {
                PrecalculateFirstTeleport();
            }
        }


        //Temp function for until I can solve this once and for all.
        void CurrentMoveDebug()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                //Current guess for what happened: Latency high enough that the "phase change" grace period of 3 seconds isn't long enough to sync everything
                //This will act as a failsafe in that case, giving it a "default" move to fall back to while it waits to sync.
                //More importantly, it will also lets us know for sure what is happening.
                UsefulFunctions.ServerText("tsorcRevamp WARNING: High-latency connection interfering with boss AI!", Color.Orange);
                UsefulFunctions.ServerText("Relevant data: Netmode: Server nextAttack: " + ((ActiveMoveList != null) ? ActiveMoveList[NextAttackMode].Name : "NULL!") + " firstPhase: " + firstPhase + " AttackModeCounter: " + AttackModeCounter, Color.Yellow);
                if (ActiveMoveList != null)
                {
                    UsefulFunctions.ServerText("ActiveMoveList :" + ActiveMoveList, Color.Yellow);
                }
                else
                {
                    UsefulFunctions.ServerText("ActiveMoveList :" + "NULL!", Color.Red);
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //Current guess for what happened: Latency high enough that the "phase change" grace period of 3 seconds isn't long enough to sync everything
                //This will act as a failsafe in that case, giving it a "default" move to fall back to while it waits to sync.
                //More importantly, it will also lets us know for sure what is happening.
                Main.NewText("tsorcRevamp WARNING: High-latency connection interfering with boss AI!", Color.Orange);
                Main.NewText("Relevant data: Netmode: Client nextAttack: " + ((ActiveMoveList != null) ? ActiveMoveList[NextAttackMode].Name : "NULL!") + " firstPhase: " + firstPhase + " AttackModeCounter: " + AttackModeCounter, Color.Yellow);
                if (ActiveMoveList != null)
                {
                    Main.NewText("ActiveMoveList :" + ActiveMoveList, Color.Yellow);
                }
                else
                {
                    Main.NewText("ActiveMoveList :" + "NULL!", Color.Red);
                }
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                //This shouldn't happen. It's never happened during testing. The same *was* supposed to be true of the above too, though. I'm adding it just in-case.
                Main.NewText("tsorcRevamp ERROR: Dark Cloud Move not set! Please report this!!", Color.Red);
                Main.NewText("Relevant data: nextAttack: " + ((ActiveMoveList != null) ? ActiveMoveList[NextAttackMode].Name : "NULL!") + " firstPhase: " + firstPhase + " AttackModeCounter: " + AttackModeCounter, Color.Yellow);
                if (ActiveMoveList != null)
                {
                    Main.NewText("ActiveMoveList :" + ActiveMoveList, Color.Yellow);
                }
                else
                {
                    Main.NewText("ActiveMoveList :" + "NULL!", Color.Red);
                }

            }
        }

        //Randomly pick a new unused attack and reset attack variables
        void ChangeAttacks()
        {
            if (testAttack == -1)
            {
                for (int i = 0; i < ActiveMoveList.Count; i++)
                {
                    if (ActiveMoveList[i].ID == NextAttackMode)
                    {
                        //Set the current move using the previous, stored attack mode now that it's had time to sync
                        CurrentMove = ActiveMoveList[i];

                        //Remove the chosen attack from the list so it can't be picked again until all other attacks are used up
                        ActiveMoveList.RemoveAt(i);
                        break;
                    }
                    if (i == (ActiveMoveList.Count - 1) && Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText("Move failed to set! NextAttackMode " + NextAttackMode + "ActiveMoveList.Count " + ActiveMoveList.Count);
                    }
                }
                if (CurrentMove == null)
                {
                    CurrentMoveDebug();
                }

                //If there's no moves left in the list, refill it   
                if (ActiveMoveList.Count == 0)
                {
                    InitializeMoves();
                }

                //Pick the next attack mode from the ones that remain, and store it in ai[0] (NextAttackMode) so it can sync
                NextAttackMode = ActiveMoveList[Main.rand.Next(ActiveMoveList.Count)].ID;
            }
            else
            {
                CurrentMove = ActiveMoveList[testAttack];
                NextAttackMode = testAttack;
            }

            //Reset variables
            npc.velocity = Vector2.Zero;
            AttackModeCounter = -1;
            AttackModeTally = 0;
            nextWarpPoint = Vector2.Zero;
            InstantNetUpdate();
        }


        //Tells the game to sync the NPC's data *now* instead of waiting until the end of AI() like npc.netUpdate = true;
        void InstantNetUpdate()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, this.npc.whoAmI);
            }
        }

        void InitializeMoves(List<int> validMoves = null)
        {
            DefaultList = new List<LeonhardMove>
            {
                new LeonhardMove(CloseSlashMove, CloseSlashAttack, LeonhardAttackID.CloseSlash, "Close Slash"),
                new LeonhardMove(BeamSlashesMove, BeamSlashesAttack, LeonhardAttackID.BeamSlashes, "Beam Slashes"),
                new LeonhardMove(FirebombThrowMove, FirebombThrowAttack, LeonhardAttackID.FirebombThrow, "Firebomb Throw"),

            };

            ActiveMoveList = new List<LeonhardMove>();
            List<LeonhardMove> TempList = DefaultList;

            if (validMoves != null)
            {
                for (int i = 0; i < TempList.Count; i++)
                {
                    if (validMoves.Contains(TempList[i].ID))
                    {
                        ActiveMoveList.Add(TempList[i]);
                    }
                }
            }
            else
            {
                ActiveMoveList = TempList;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            //Send the list of remaining moves
            if (ActiveMoveList == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(ActiveMoveList.Count);
                for (int i = 0; i < ActiveMoveList.Count; i++)
                {
                    writer.Write(ActiveMoveList[i].ID);
                }
            }


            //A seed value that clients can use whenever they'd like to pick the next attack.
            //Would allow all clients to "randomly" roll the same attack right when it happens, instead of needing to do it early.
            //writer.Write(sendEntropy);
            //if (sendEntropy)
            // {
            //     NextWarpEntropy = Main.rand.Next();
            //     writer.Write(NextWarpEntropy);
            // }

            //Send the next point to teleport to during this attack, and the first point for the next attack
            writer.WriteVector2(nextWarpPoint);
            writer.WriteVector2(preSelectedWarpPoint);
            ;

            if (CurrentMove == null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(CurrentMove.ID);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            //Recieve the list of remaining moves
            int moveCount = reader.ReadInt32();
            List<int> validMoves = new List<int>();
            for (int i = 0; i < moveCount; i++)
            {
                int move = reader.ReadInt32();
                validMoves.Add(move);
            }
            InitializeMoves(validMoves);

            //bool recievedEntropy = reader.ReadBoolean();
            //if (recievedEntropy)
            //{
            //    //A seed value that clients can use whenever they'd like to pick the next attack.
            //    NextWarpEntropy = reader.ReadInt32();
            //}

            //Recieve the next point to teleport to during this attack, and the first point for the next attack
            nextWarpPoint = reader.ReadVector2();
            preSelectedWarpPoint = reader.ReadVector2();


            int readMoveID = reader.ReadInt32();
            if (readMoveID != -1)
            {
                for (int i = 0; i < DefaultList.Count; i++)
                {
                    if (DefaultList[i].ID == readMoveID)
                    {
                        CurrentMove = DefaultList[i];
                    }
                }
            }
        }

        void TeleportToArenaCenter()
        {
            LeonhardParticleEffect(-2);
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                npc.Center = new Vector2(5827.5f, 1698) * 16;
            }
            else
            {
                Vector2 warpPoint = Target.Center;
                warpPoint.Y -= 600;
                npc.Center = warpPoint;
            }
            LeonhardParticleEffect(6);
            InstantNetUpdate();
        }

        //The dust ring particle effect the boss uses
        void LeonhardParticleEffect(float dustSpeed, float dustAmount = 50, float radius = 64)
        {
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = new Vector2(dustSpeed, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(npc.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
            }
        }

        //A charging effect that focuses in on dark cloud and grows in intensity as time goes on
        void ChargingParticleEffect(float progress, float maxProgress)
        {
            float count = (progress / maxProgress) * 30;
            LeonhardParticleEffect(-5, count * 4, 42 - count);
        }

        #region Vanilla overrides and misc
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }
        //Takes double damage from melee weapons
        /*public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            damage *= 2;
            crit = true;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.melee)
            {
                damage *= 2;
                crit = true;
            }
        }*/

    /*
        #endregion



        //These describe how the boss should move, and other things that should be done on the server and every client to keep it deterministic
        #region Movements


        //A few moves use teleports that need to be calculated in advance so their first warp can be pre-synced. That's done here.
        void PrecalculateFirstTeleport()
        {
            /*if (NextAttackMode == DarkCloudAttackID.DivineSpark)
            {
                preSelectedWarpPoint = DivineSparkTeleport();
            }*/
    /*
    InstantNetUpdate();
        }

        void CloseSlashMove()
        {
            if (AttackModeCounter < 120)
            {
                npc.velocity.X = 0;
            }
            else if (AttackModeCounter < 180)
            {
                npc.velocity.X = 4;
            }
            else if (AttackModeCounter < 240)
            {
                npc.velocity.X = -4;
            }
            /*else if (AttackModeCounter < 300)
            {
                npc.velocity.X = 4;
            }
            else if (AttackModeCounter < 360)
            {
                npc.velocity.X = -4;
            }*/
    /*
            else
            {
                ChangeAttacks();
            }
        }

        void BeamSlashesMove()
        {
            if (AttackModeCounter < 120)
            {
                npc.velocity.Y = 0;
            }
            else if (AttackModeCounter < 180)
            {
                npc.velocity.Y = 4;
            }
            else if (AttackModeCounter < 240)
            {
                npc.velocity.Y = -4;
            }
            /*else if (AttackModeCounter < 300)
            {
                npc.velocity.Y = 4;
            }
            else if (AttackModeCounter < 360)
            {
                npc.velocity.Y = -4;
            }*/
    /*
            else
            {
                ChangeAttacks();
            }


        }

        void DrinkPotionMove()
        {



        }

        void FirebombThrowMove()
        {
            npc.TargetClosest(true);
            Vector2 targetPosition = Main.player[npc.target].position; // get a local copy of the targeted player's position
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));

            if (AttackModeCounter < 400)
            {
                if (Main.player[npc.target].position.X < vector8.X)
                {
                    if (npc.velocity.X > -7) { npc.velocity.X -= 0.22f; }
                }
                if (Main.player[npc.target].position.X > vector8.X)
                {
                    if (npc.velocity.X < 7) { npc.velocity.X += 0.22f; }
                }

                if (Main.player[npc.target].position.Y < vector8.Y + 200)
                {
                    if (npc.velocity.Y > 0f) npc.velocity.Y -= 0.8f;
                    else npc.velocity.Y -= 0.07f;
                }
                if (Main.player[npc.target].position.Y > vector8.Y + 200)
                {
                    if (npc.velocity.Y < 0f) npc.velocity.Y += 0.8f;
                    else npc.velocity.Y += 0.07f;
                }
            }
            else
            {
                ChangeAttacks();
            }


        }

        void HomingSoulmassMove()
        {



        }

        void GunfireMove()
        {



        }



        #endregion


        //These describe projectiles the boss should shoot, and other things that should *not* be done for every multiplayer client
        #region Attacks


        void CloseSlashAttack()
        {



        }

        void BeamSlashesAttack()
        {



        }

        void DrinkPotionAttack()
        {



        }

        void FirebombThrowAttack()
        {
            if (AttackModeCounter % 20 == 0)
            {
                Projectile.NewProjectile(npc.Center, new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, -3)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), FirebombDamage, 1, Main.myPlayer);
            }


        }

        void HomingSoulmassAttack()
        {



        }

        void GunfireAttack()
        {



        }


        #endregion




        #endregion



        #region Drawing and Animation



        static Texture2D leonhardBodyTexture = ModContent.GetTexture("tsorcRevamp/NPCs/Special/MoonlitLeonhard");
        static Texture2D leonhardMaskTexture = ModContent.GetTexture("tsorcRevamp/NPCs/Special/MoonlitLeonhard_Mask");


        public override void DrawEffects(ref Color drawColor)
        {
            if (Main.rand.Next(6) == 0)
            {
                int num5 = Dust.NewDust(npc.position, npc.width, npc.height, 89, 0f, 0f, 120, default, .5f);
                Main.dust[num5].noGravity = true;
                Main.dust[num5].velocity *= 0f;
                Main.dust[num5].fadeIn = 1.2f;
                Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                vector.Normalize();
                vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                Main.dust[num5].velocity = vector;
                vector.Normalize();
                vector *= Main.rand.Next(220, 900);
                Main.dust[num5].position = npc.Center - vector;
                Main.dust[num5].velocity *= 0f;

            }

            base.DrawEffects(ref drawColor);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (npc.spriteDirection == -1)
            {
                spriteBatch.Draw(leonhardBodyTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 110, 114), drawColor, npc.rotation, new Vector2(56, 56), npc.scale, effects, 0.1f);
                spriteBatch.Draw(leonhardMaskTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 110, 114), drawColor, npc.rotation, new Vector2(56, 56), npc.scale, effects, 0.1f);

            }
            else
            {
                spriteBatch.Draw(leonhardBodyTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 110, 114), drawColor, npc.rotation, new Vector2(56, 56), npc.scale, effects, 0.1f);
                spriteBatch.Draw(leonhardMaskTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 110, 114), drawColor, npc.rotation, new Vector2(56, 56), npc.scale, effects, 0.1f);

            }



            return false;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            base.PostDraw(spriteBatch, drawColor);
        }


        public override void FindFrame(int frameHeight)
        {
            npc.rotation = npc.velocity.X * 0.03f;
            npc.spriteDirection = npc.direction;
            npc.frameCounter++;
            if (npc.frameCounter < 12)
            {
                npc.frame.Y = 0 * frameHeight;
            }
            else if (npc.frameCounter < 24)
            {
                npc.frame.Y = 1 * frameHeight;
            }
            else if (npc.frameCounter < 36)
            {
                npc.frame.Y = 2 * frameHeight;
            }
            else if (npc.frameCounter < 48)
            {
                npc.frame.Y = 3 * frameHeight;
            }
            else if (npc.frameCounter < 60)
            {
                npc.frame.Y = 4 * frameHeight;
            }
            else if (npc.frameCounter < 72)
            {
                npc.frame.Y = 5 * frameHeight;
            }
            else if (npc.frameCounter < 84)
            {
                npc.frame.Y = 6 * frameHeight;
            }
            else
            {
                npc.frameCounter = 0;
            }
        }



        #endregion


        //This class exists to pair up the Move, Attack, Draw, and ID of each attack type into one nice and neat state object
        class LeonhardMove
        {
            public Action Move;
            public Action Attack;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public LeonhardMove(Action MoveAction, Action AttackAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                Attack = AttackAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }

        //So I don't have to remember magic numbers
        //Public because Dark Cloud Mirror NPC's also use it
        public class LeonhardAttackID
        {
            public const short CloseSlash = 0;
            public const short BeamSlashes = 1;
            public const short DrinkPotion = 2;
            public const short FirebombThrow = 3;
            public const short HomingSoulMass = 4;
            public const short Gunfire = 5;


        }


    }*/
}