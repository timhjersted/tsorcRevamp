using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.NPCs.Bosses.Attraidies;
using tsorcRevamp.Systems;

namespace tsorcRevamp.NPCs.Special
{
    class AttraidiesApparition : AttraidiesPuppet //Attraidies that appears in the cavern of The Sorrow
    {
        private enum SceneStage : byte
        {
            Voice,
            Talking,
            Charging,
        }

        private const string AttraidiesName = "Cutscenes.Speakers.Attraidies";
        private const string RedName = "Cutscenes.Speakers.Red";
        private const string VoiceName = "Cutscenes.Speakers.Voice";
        private const int LineCount = 16;
        private const int LineAutoTicks = 12 * 60;

        // Line indexes with staging attached: Red's silent glare follows line 3, the zoom leads into line 14.
        private const int SilentGlareBeforeLine = 4;
        private const int ZoomBeforeLine = 14;

        private const int AppearPauseTicks = 45;
        private const int GlarePauseTicks = 45;
        private const int ZoomPauseTicks = 60;
        private const int NormalPauseTicks = 8;
        private const float ConfessionZoom = 1.4f;
        private const int ZoomOutTicks = 90;

        // Charge thresholds. A retry starts near the end, so the fight begins almost at once.
        private const int ChargeLightDustTick = 40;
        private const int ChargeMediumDustTick = 90;
        private const int ChargeHeavyDustTick = 130;
        private const int ChargeEndTick = 180;
        private const int RetryChargeStartTick = 155;

        // Speaking gestures: each new line plays one for this long, cycling hand, staff lift, then pointing at Red.
        private const int GestureTicks = 50;
        private const int ChargeOverheadTick = 120;
        private const float OpenHandAngle = -0.3f;

        private SceneStage stage = SceneStage.Voice;
        private SceneStage lastSeenStage = SceneStage.Voice;
        private bool sceneStarted;
        private int leadPlayer = -1;
        private int lineIndex;
        private int pauseTicks;
        private bool restoreFocusAfterPause;
        private int gestureLine = -1;
        private int gestureTimer;

        protected override bool ShowsTeleportFlash => false;

        protected override Vector2 FacingPoint
        {
            get
            {
                if (leadPlayer >= 0)
                {
                    return Main.player[leadPlayer].Center;
                }

                return NPC.Center + new Vector2(NPC.direction * 100f, 0f);
            }
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.npcSlots = 5;
            NPC.lifeMax = 80000;
            NPC.defense = 10;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.alpha = 255;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)stage);
            writer.Write((byte)lineIndex);
            writer.Write((short)leadPlayer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            stage = (SceneStage)reader.ReadByte();
            lineIndex = reader.ReadByte();
            leadPlayer = reader.ReadInt16();
        }

        private void SetStage(SceneStage newStage)
        {
            stage = newStage;
            NPC.netUpdate = true;
        }

        private void ShowCurrentLine()
        {
            int lineNumber = lineIndex + 1;
            string textKey = "Cutscenes.Sorrow.Line" + lineNumber.ToString("00");
            CutsceneLine line;

            if (lineIndex == 0)
            {
                line = CutsceneLine.FromNobody(VoiceName, textKey);
            }
            else if (lineIndex == 2)
            {
                line = CutsceneLine.FromPlayer(leadPlayer, RedName, textKey);
            }
            else
            {
                line = CutsceneLine.FromNpc(NPC.whoAmI, AttraidiesName, textKey);
            }

            CutsceneSystem.ShowLine(line, LineAutoTicks);
        }

        #region AI

        protected override void TickForm()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            // Poses come from the synced stage and line, so every machine plays the same gesture.
            if (lineIndex != gestureLine)
            {
                gestureLine = lineIndex;
                gestureTimer = 0;
            }

            gestureTimer++;
            FreeHandGesture = null;
            SetPose(AttraidiesPose.Carry);

            if (stage == SceneStage.Charging)
            {
                if (NPC.ai[2] < ChargeOverheadTick)
                {
                    SetPose(AttraidiesPose.Conduct);
                }
                else
                {
                    SetPose(AttraidiesPose.Overhead);
                }
            }
            else if (stage == SceneStage.Talking && lineIndex <= 1)
            {
                SetPose(AttraidiesPose.Overhead);
            }
            else if (stage == SceneStage.Talking && gestureTimer < GestureTicks)
            {
                int gesture = lineIndex % 3;
                if (gesture == 0)
                {
                    FreeHandGesture = OpenHandAngle;
                }
                else if (gesture == 1)
                {
                    SetPose(AttraidiesPose.Overhead);
                }
                else if (leadPlayer >= 0)
                {
                    SetAimPose(Main.player[leadPlayer].Center);
                }
            }

            if (authority && !sceneStarted)
            {
                sceneStarted = true;

                if (CutsceneSystem.HasSeen(AttraidiesCutscenes.SorrowScene))
                {
                    // The confession plays once per world. A retry only needs the short burst into the fight.
                    NPC.ai[2] = RetryChargeStartTick;
                    SetStage(SceneStage.Charging);
                }
                else
                {
                    ScriptedEvent ownerEvent = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ScriptedEventOwner;
                    leadPlayer = NPC.FindClosestPlayer();
                    if (ownerEvent != null && ownerEvent.TriggeringPlayer >= 0)
                    {
                        leadPlayer = ownerEvent.TriggeringPlayer;
                    }

                    NPC.netUpdate = true;

                    CutsceneCast cast = AttraidiesCutscenes.BuildCast(NPC.Center, leadPlayer, AttraidiesCutscenes.SorrowScene);
                    CutsceneFocus focus = CutsceneFocus.BetweenLocalPlayerAndNpc(NPC.whoAmI);
                    CutsceneSystem.Start(AttraidiesCutscenes.SorrowScene, AttraidiesCutscenes.OpenEndedSceneTicks, focus, NPC.whoAmI, cast);
                    ShowCurrentLine();
                }
            }

            // He has no body until his first line has been heard.
            bool visible = stage != SceneStage.Voice;
            NPC.alpha = 255;
            if (visible)
            {
                NPC.alpha = 0;
                Lighting.AddLight(NPC.Center, 1f, 0.75f, 1f);

                int dust1 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 180, Color.Blue, 1f);
                Main.dust[dust1].noGravity = true;
            }

            // Every machine sees the stage change at its own moment, so the arrival burst and laugh are driven from here.
            if (stage != lastSeenStage && lastSeenStage == SceneStage.Voice)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 1.1f }, NPC.Center);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f);
                        Dust.NewDustPerfect(NPC.Center, 54, burstVelocity, 100, default, Main.rand.NextFloat(1.2f, 1.7f)).noGravity = true;
                        Dust.NewDustPerfect(NPC.Center, 29, burstVelocity * 0.7f, 100, default, Main.rand.NextFloat(1.2f, 1.7f)).noGravity = true;
                    }
                }

                if (authority)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, 0), ModContent.ProjectileType<Barrier>(), 0, 0, Main.myPlayer, 1);
                }
            }
            lastSeenStage = stage;

            //"Movement"

            if (NPC.ai[0] == 0f)
            {
                if (NPC.velocity.Length() < 0.1)
                {
                    NPC.velocity.X = 0f;
                    NPC.velocity.Y = 0f;
                    NPC.ai[0] = 1f;
                    NPC.ai[1] = 45f;
                    return;
                }

                NPC.velocity *= 0.94f;
                if (NPC.velocity.X < 0f)
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }

                NPC.spriteDirection = NPC.direction;
                return;
            }

            NPC.spriteDirection = NPC.direction;
            NPC.ai[1] += 1f;
            float acceleration = 0.005f;
            if (NPC.ai[1] > 0f)
            {
                NPC.velocity.Y -= acceleration;
            }
            else
            {
                NPC.velocity.Y += acceleration;
            }

            if (NPC.ai[1] >= 90f)
            {
                NPC.ai[1] *= -1f;
            } // End Movement

            // Dialogue: runs on the server, one line at a time, with short staged pauses between lines.
            if (authority && stage != SceneStage.Charging)
            {
                // Skip jumps to the retry's short charge, so The Sorrow still spawns and the scene is still marked seen.
                bool skipPressed = CutsceneSystem.SkipRequested && CutsceneSystem.SceneId == AttraidiesCutscenes.SorrowScene;
                if (skipPressed)
                {
                    const int SkipCloseTicks = 20;
                    CutsceneSystem.SetSkipAllowed(false);
                    CutsceneSystem.ClearLine();
                    pauseTicks = 0;
                    restoreFocusAfterPause = false;
                    lineIndex = LineCount;
                    NPC.ai[2] = RetryChargeStartTick;
                    CutsceneSystem.SetZoom(1f, SkipCloseTicks);
                    CutsceneSystem.SetFocus(CutsceneFocus.BetweenLocalPlayerAndNpc(NPC.whoAmI), SkipCloseTicks);
                    CutsceneSystem.FinishIn(ChargeEndTick - RetryChargeStartTick + 1);
                    SetStage(SceneStage.Charging);
                }
                else if (pauseTicks > 0)
                {
                    pauseTicks--;
                    if (pauseTicks == 0)
                    {
                        if (restoreFocusAfterPause)
                        {
                            restoreFocusAfterPause = false;
                            CutsceneSystem.SetFocus(CutsceneFocus.BetweenLocalPlayerAndNpc(NPC.whoAmI));
                        }

                        ShowCurrentLine();
                    }
                }
                else if (CutsceneSystem.LineFinished)
                {
                    CutsceneSystem.ClearLine();
                    lineIndex++;
                    NPC.netUpdate = true;
                    pauseTicks = NormalPauseTicks;

                    if (lineIndex == 1)
                    {
                        SetStage(SceneStage.Talking);
                        pauseTicks = AppearPauseTicks;
                    }
                    else if (lineIndex == SilentGlareBeforeLine)
                    {
                        // "Red is silent. Glaring." The camera holds on Red with no text.
                        CutsceneSystem.SetFocus(CutsceneFocus.OnPlayer(leadPlayer));
                        restoreFocusAfterPause = true;
                        pauseTicks = GlarePauseTicks;
                    }
                    else if (lineIndex == ZoomBeforeLine)
                    {
                        CutsceneSystem.SetFocus(CutsceneFocus.OnNpc(NPC.whoAmI));
                        CutsceneSystem.SetZoom(ConfessionZoom, ZoomPauseTicks);
                        pauseTicks = ZoomPauseTicks;
                    }
                    else if (lineIndex >= LineCount)
                    {
                        pauseTicks = 0;
                        CutsceneSystem.SetZoom(1f, ZoomOutTicks);
                        CutsceneSystem.SetFocus(CutsceneFocus.BetweenLocalPlayerAndNpc(NPC.whoAmI));
                        CutsceneSystem.FinishIn(ChargeEndTick + 1);
                        SetStage(SceneStage.Charging);
                    }
                }
            }

            // Event duration & The Sorrow Spawning

            if (stage != SceneStage.Charging)
            {
                return;
            }

            int lifetime = (int)NPC.ai[2];
            ++NPC.ai[2];

            if (lifetime > ChargeLightDustTick && lifetime < ChargeMediumDustTick)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > ChargeMediumDustTick && lifetime < ChargeHeavyDustTick)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > ChargeHeavyDustTick)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }

            if (lifetime > ChargeEndTick && NPC.ai[3] == 0f)
            {
                //One-shot: only spawn The Sorrow once, even if this intro gets revived (ai[3] is unused here).
                NPC.ai[3] = 1f;

                for (int i = 0; i < 50; i++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-50, 50) * 2, Main.rand.Next(-100, 100) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (authority)
                {
                    CutsceneSystem.MarkSeen(AttraidiesCutscenes.SorrowScene);
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.Birds.TheSorrow>());
                }
                NPC.active = false;

            }
        }
        #endregion
    }
}
