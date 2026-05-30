using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.EnemySpriteRendering
{
    public enum EnemySpritePose
    {
        Natural,
        Carry,
        ThrowTelegraph,
        ThrowRelease,
        MagicTelegraph,
        MagicRelease,
        MagicAim
    }

    public enum EnemyHeldItemStyle
    {
        Generic,
        Spear,
        Bomb,
        MagicBall
    }

    public class EnemySpriteRenderer
    {
        private const int FrameHeight = 56;
        private const int WeaponAnimMax = 22;

        internal static EnemySpriteRenderer DrawingRendererFor;
        internal static NPC DrawingNPC;

        private readonly int headArmorItemType;
        private readonly int bodyArmorItemType;
        private readonly int legsArmorItemType;
        private readonly Dictionary<int, Item> itemCache = new();
        private readonly Dictionary<string, Texture2D> textureCache = new();

        private Player puppet;
        private float frameCounter;
        private int heldItemType = -1;
        private bool weaponVisible;
        private EnemySpritePose pose;
        private int poseTimer;
        private int poseDuration;
        private float weaponRotation;
        private int weaponAnim;
        private EnemyHeldItemStyle heldItemStyle;
        private string heldTexturePath;
        private float? targetDrawRotation;
        private Vector2 handleNorm = new(0.10f, 0.85f);

        // Wings — populated lazily on SetWings.
        private int wingsItemType = -1;
        private int wingSlotCached;
        private EnemyFlightController flightRef;

        internal Color LayerDrawColor { get; private set; }

        public EnemySpriteRenderer(int headArmorItemType, int bodyArmorItemType, int legsArmorItemType)
        {
            this.headArmorItemType = headArmorItemType;
            this.bodyArmorItemType = bodyArmorItemType;
            this.legsArmorItemType = legsArmorItemType;
        }

        public void SetHandleNorm(Vector2 value) => handleNorm = value;

        /// <summary>
        /// Equip wings on the puppet.  When a flight controller is supplied, its
        /// <see cref="EnemyFlightController.WingsActiveThisTick"/> drives the vanilla wing
        /// flap layer (set as <c>puppet.controlJump</c>), so flap-and-glide cadence matches
        /// what the controller computes.
        /// </summary>
        /// <param name="wingsItemType">Vanilla wing item type, e.g. <c>ItemID.LeatherWings</c>.</param>
        /// <param name="flight">Optional flight controller for animation cadence.  Null = static spread.</param>
        public void SetWings(int wingsItemType, EnemyFlightController flight = null)
        {
            this.wingsItemType = wingsItemType;
            this.flightRef = flight;
            if (wingsItemType > 0)
            {
                var probe = new Item();
                probe.SetDefaults(wingsItemType);
                wingSlotCached = probe.wingSlot;
            }
            else
            {
                wingSlotCached = 0;
            }
            // If puppet exists already, push the slot now.
            if (puppet != null)
            {
                puppet.wings = wingSlotCached;
                puppet.wingTimeMax = 200;
            }
        }

        public void SetPose(EnemySpritePose pose, int heldItemType = -1, bool weaponVisible = true, int poseTimer = 0, int poseDuration = 1, EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic, string heldTexturePath = null, float? targetDrawRotation = null)
        {
            bool poseChanged = this.pose != pose || this.heldItemType != heldItemType;
            this.pose = pose;
            this.heldItemType = heldItemType;
            this.weaponVisible = weaponVisible && heldItemType > 0;
            this.poseTimer = Math.Max(poseTimer, 0);
            this.poseDuration = Math.Max(poseDuration, 1);
            this.heldItemStyle = heldItemStyle;
            this.heldTexturePath = heldTexturePath;
            this.targetDrawRotation = targetDrawRotation;

            if (poseChanged && (pose == EnemySpritePose.ThrowRelease || pose == EnemySpritePose.MagicRelease))
            {
                weaponAnim = WeaponAnimMax;
            }
        }

        public bool Draw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (puppet == null)
            {
                InitPuppet();
            }

            SyncPuppet(npc);
            LayerDrawColor = drawColor;

            DrawingRendererFor = this;
            DrawingNPC = npc;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, puppet, npc.position, 0f, Vector2.Zero);
            DrawingRendererFor = null;
            DrawingNPC = null;

            return false;
        }

        private void InitPuppet()
        {
            puppet = new Player
            {
                active = true,
                whoAmI = 0,
                gravDir = 1f,
                Male = true
            };

            puppet.armor[0] = new Item();
            puppet.armor[0].SetDefaults(headArmorItemType);
            puppet.armor[1] = new Item();
            puppet.armor[1].SetDefaults(bodyArmorItemType);
            puppet.armor[2] = new Item();
            puppet.armor[2].SetDefaults(legsArmorItemType);

            puppet.head = puppet.armor[0].headSlot;
            puppet.body = puppet.armor[1].bodySlot;
            puppet.legs = puppet.armor[2].legSlot;

            puppet.skinColor = Color.Black;
            puppet.eyeColor = Color.Black;
            puppet.hairColor = Color.Black;
            puppet.shirtColor = Color.Black;
            puppet.underShirtColor = Color.Black;
            puppet.pantsColor = Color.Black;
            puppet.shoeColor = Color.Black;
        }

        private void SyncPuppet(NPC npc)
        {
            puppet.position = npc.position;
            puppet.velocity = npc.velocity;
            puppet.direction = npc.direction;
            puppet.width = npc.width;
            puppet.height = npc.height;
            puppet.gravDir = 1f;
            puppet.selectedItem = 0;
            puppet.itemAnimationMax = WeaponAnimMax;

            // Wings: drive vanilla wing layer animation from the flight controller.
            if (wingsItemType > 0)
            {
                puppet.wings = wingSlotCached;
                if (flightRef != null && flightRef.IsAirborne)
                {
                    puppet.wingTime = puppet.wingTimeMax;
                    puppet.controlJump = flightRef.WingsActiveThisTick;
                    puppet.wingFrame = (int)(flightRef.WingAnimPhase * 4f) % 4;
                }
                else
                {
                    puppet.wingTime = 0;
                    puppet.controlJump = false;
                }
            }

            bool armPoseActive = IsWeaponPosePhase;
            if (armPoseActive && heldItemType > 0)
            {
                puppet.inventory[0] = GetCachedItem(heldItemType);
                puppet.itemAnimation = Math.Max(weaponAnim, 1);
                puppet.itemRotation = npc.direction * weaponRotation;
            }
            else
            {
                puppet.inventory[0] = new Item();
                puppet.itemAnimation = 0;
                puppet.itemRotation = 0f;
            }

            SyncFrames(npc);
            TickWeaponRotation();
            SyncCompositeArm(npc);
        }

        private bool IsWeaponPosePhase => pose != EnemySpritePose.Natural && pose != EnemySpritePose.Carry;

        private Item GetCachedItem(int itemType)
        {
            if (!itemCache.TryGetValue(itemType, out Item item))
            {
                item = new Item();
                item.SetDefaults(itemType);
                item.noUseGraphic = true;
                itemCache[itemType] = item;
            }

            return item;
        }

        private void SyncFrames(NPC npc)
        {
            bool onGround = npc.velocity.Y == 0f;
            bool moving = Math.Abs(npc.velocity.X) > 0.15f;

            if (onGround && moving && !Main.gamePaused)
            {
                frameCounter += Math.Abs(npc.velocity.X) * 0.55f;
                if (frameCounter >= 14f)
                {
                    frameCounter = 0f;
                }
            }

            int bodyRow = pose switch
            {
                EnemySpritePose.ThrowTelegraph => GetBodyRowFromWeaponPitch(),
                EnemySpritePose.ThrowRelease => 3,
                EnemySpritePose.MagicTelegraph => GetBodyRowFromWeaponPitch(),
                EnemySpritePose.MagicRelease => 3,
                EnemySpritePose.MagicAim => 3,
                _ when !onGround => 5,
                _ when moving => 6 + (int)frameCounter,
                _ => 0
            };

            int legRow;
            if (!onGround)
            {
                legRow = 5;
            }
            else if (moving)
            {
                legRow = 6 + (int)frameCounter;
            }
            else
            {
                legRow = 0;
            }

            puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            puppet.legFrame = new Rectangle(0, FrameHeight * legRow, 40, FrameHeight);
        }

        private void TickWeaponRotation()
        {
            if (weaponAnim > 0)
            {
                weaponAnim--;
            }

            float progress = GetPoseProgress();
            weaponRotation = pose switch
            {
                EnemySpritePose.Carry => MathHelper.Lerp(weaponRotation, GetCarryRotation(), 0.18f),
                EnemySpritePose.ThrowTelegraph => GetThrowTelegraphRotation(progress),
                EnemySpritePose.ThrowRelease => MathHelper.Lerp(-0.10f, 0.45f, progress),
                EnemySpritePose.MagicTelegraph => MathHelper.Lerp(weaponRotation, -1.40f, 0.12f),
                EnemySpritePose.MagicRelease => MathHelper.Lerp(-1.40f, 0.20f, progress),
                EnemySpritePose.MagicAim => MathHelper.Lerp(weaponRotation, 0.05f, 0.22f),
                _ => MathHelper.Lerp(weaponRotation, 0.35f, 0.10f)
            };
        }

        private void SyncCompositeArm(NPC npc)
        {
            if (!IsWeaponPosePhase)
            {
                puppet.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
                return;
            }

            float armRotation = weaponRotation - MathHelper.PiOver2;

            if (npc.direction == -1)
            {
                armRotation = MathHelper.Pi - armRotation;
            }

            puppet.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        private int GetBodyRowFromWeaponPitch()
        {
            float pitch = (1f - (float)Math.Sin(weaponRotation)) / 2f;
            if (pitch > 0.95f)
            {
                return 1;
            }

            if (pitch > 0.70f)
            {
                return 2;
            }

            if (pitch > 0.30f)
            {
                return 3;
            }

            return 4;
        }

        private float GetCarryRotation()
        {
            return heldItemStyle switch
            {
                EnemyHeldItemStyle.Spear => -0.35f,
                EnemyHeldItemStyle.Bomb => -0.20f,
                _ => 0.35f
            };
        }

        private float GetThrowTelegraphRotation(float progress)
        {
            if (progress < 0.55f)
            {
                return MathHelper.Lerp(GetCarryRotation(), -1.35f, progress / 0.55f);
            }

            return MathHelper.Lerp(-1.35f, -0.10f, (progress - 0.55f) / 0.45f);
        }

        private float GetPoseProgress()
        {
            return MathHelper.Clamp(1f - (float)poseTimer / poseDuration, 0f, 1f);
        }

        private Vector2 GetHandPosition(NPC npc)
        {
            if (IsWeaponPosePhase)
            {
                float armRotation = weaponRotation - MathHelper.PiOver2;
                if (npc.direction == -1)
                {
                    armRotation = MathHelper.Pi - armRotation;
                }
                Vector2 handPos = puppet.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
                handPos.Y += puppet.gfxOffY;
                return handPos;
            }

            int bodyRow = puppet.bodyFrame.Y / FrameHeight;
            if (pose == EnemySpritePose.Carry)
            {
                Vector2 carryOffset = GetCarryHandOffset(bodyRow);
                return npc.Center + new Vector2(carryOffset.X * npc.direction, carryOffset.Y);
            }

            Vector2 offset = bodyRow switch
            {
                1 => new Vector2(-8f, -9f),
                2 => new Vector2(4f, -8f),
                3 => new Vector2(4f, 2f),
                4 => new Vector2(4f, 7f),
                _ => new Vector2(4f, 2f)
            };

            return npc.Center + new Vector2(offset.X * npc.direction, offset.Y);
        }

        private static Vector2 GetCarryHandOffset(int bodyRow)
        {
            if (bodyRow == 5)
            {
                return new Vector2(7f, 0f);
            }

            if (bodyRow < 6)
            {
                return new Vector2(7f, 4f);
            }

            int walkFrame = Math.Max(0, Math.Min(13, bodyRow - 6));
            return walkFrame switch
            {
                0 => new Vector2(7f, 4f),
                1 => new Vector2(8f, 5f),
                2 => new Vector2(8f, 6f),
                3 => new Vector2(7f, 7f),
                4 => new Vector2(6f, 7f),
                5 => new Vector2(5f, 6f),
                6 => new Vector2(4f, 5f),
                7 => new Vector2(4f, 4f),
                8 => new Vector2(5f, 3f),
                9 => new Vector2(6f, 3f),
                10 => new Vector2(7f, 3f),
                11 => new Vector2(8f, 4f),
                12 => new Vector2(8f, 5f),
                _ => new Vector2(7f, 5f),
            };
        }

        internal void DrawWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!weaponVisible || heldItemType <= 0 || puppet == null)
            {
                return;
            }

            Texture2D texture = GetHeldTexture();
            if (texture == null)
            {
                return;
            }

            NPC npc = DrawingNPC;
            if (npc == null || !npc.active)
            {
                return;
            }

            Vector2 handPosition = GetHandPosition(npc) - Main.screenPosition;
            bool centeredGrip = heldItemStyle == EnemyHeldItemStyle.Bomb || heldItemStyle == EnemyHeldItemStyle.MagicBall;

            Vector2 origin;
            if (centeredGrip)
            {
                origin = texture.Bounds.Center.ToVector2();
            }
            else if (heldItemStyle == EnemyHeldItemStyle.Spear)
            {
                origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            }
            else
            {
                float originX = npc.direction == 1
                    ? texture.Width * handleNorm.X
                    : texture.Width * (1f - handleNorm.X);
                origin = new Vector2(originX, texture.Height * handleNorm.Y);
            }

            SpriteEffects effects = npc.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float scale = heldItemStyle switch
            {
                EnemyHeldItemStyle.Bomb => 0.58f,
                EnemyHeldItemStyle.MagicBall => 0.85f * 0.75f,
                _ => 0.62f
            };
            float rotation = GetWeaponDrawRotation();

            drawInfo.DrawDataCache.Add(new DrawData(
                texture,
                handPosition,
                null,
                LayerDrawColor,
                rotation,
                origin,
                npc.scale * scale,
                effects,
                0));
        }

        private float GetWeaponDrawRotation()
        {
            if (heldItemStyle == EnemyHeldItemStyle.Spear && targetDrawRotation.HasValue)
            {
                return targetDrawRotation.Value;
            }

            return weaponRotation + (heldItemStyle == EnemyHeldItemStyle.Spear ? -MathHelper.PiOver2 : 0f);
        }

        private Texture2D GetHeldTexture()
        {
            if (!string.IsNullOrEmpty(heldTexturePath))
            {
                if (!textureCache.TryGetValue(heldTexturePath, out Texture2D texture))
                {
                    texture = ModContent.Request<Texture2D>(heldTexturePath).Value;
                    textureCache[heldTexturePath] = texture;
                }

                return texture;
            }

            return TextureAssets.Item[heldItemType].Value;
        }
    }
}
