using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static tsorcRevamp.VariousConstants;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Buffs;
using System;
using tsorcRevamp.UI;
using Microsoft.Xna.Framework.Graphics;
using TerraUI.Objects;
using Terraria.UI;
using ReLogic.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Audio;
using tsorcRevamp.Projectiles.Pets;
using static tsorcRevamp.TransparentTextureHandler;

namespace tsorcRevamp
{
    public class tsorcRevampPlayer : ModPlayer
    {

        public int warpX;
        public int warpY;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public bool SilverSerpentRing = false;
        public bool DragonStone = false;
        public int SoulReaper = 5;

        public bool DuskCrownRing = false;
        public bool UndeadTalisman = false;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;

        public bool GemBox = false;
        public bool ConditionOverload = true;

        public int CurseLevel = 1;
        public int PowerfulCurseLevel = 1;
        public bool DarkInferno = false;
        public bool CrimsonDrain = false;
        public int count = 0;

        public bool Shockwave = false;
        public bool Falling;
        public int StopFalling;
        public float FallDist;
        public float fallStartY;
        public int fallStart_old = -1;

        public bool MeleeArmorVamp10 = false;
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;
        public int ConsSoulChanceMult;
        public bool SoulSickle = false;

        public int souldroplooptimer = 0;
        public int souldroptimer = 0;
        public bool SOADrain = false;

        public int supersonicLevel = 0;

        public int darkSoulQuantity;

        //An int because it'll probably be necessary to split it into multiple levels
        public int manaShield = 0;
        //How many more frames the Mana Shield is disabled after using a mana potion
        public int manaShieldCooldown = 0;
        //What frame of the shield's animation it's on
        public int shieldFrame = 0;
        //Did they have the shield up last frame?
        public bool shieldUp = false;

        public bool chestBankOpen;
        public int chestBank = -1;

        public bool chestPiggyOpen;
        public int chestPiggy = -1;

        public int FracturingArmor = 1;

        public int dragonMorphDamage = 45;

        public int MiakodaEffectsTimer;

        public bool MiakodaFull; //Miakoda - Full Moon Form
        public bool MiakodaFullHeal1;
        public bool MiakodaFullHeal2;

        public bool MiakodaCrescent; //Miakoda - Crescent Moon Form
        public bool MiakodaCrescentBoost;
        public int MiakodaCrescentBoostTimer;
        public bool MiakodaCrescentDust1;
        public bool MiakodaCrescentDust2;

        public bool MiakodaNew; //Miakoda - New Moon Form
        public bool MiakodaNewBoost;
        public int MiakodaNewBoostTimer;
        public bool MiakodaNewDust1;
        public bool MiakodaNewDust2;

        internal bool gotPickaxe;
        public bool FirstEncounter;
        public bool ReceivedGift;

        public bool[] PermanentBuffToggles;
        public static Dictionary<int, float> DamageDir;

        public bool GiveBossZen;
        public bool BossZenBuff;

        public bool MagicWeapon;
        public bool GreatMagicWeapon;
        public bool CrystalMagicWeapon;
        public bool DarkmoonCloak;

        //increased grab range immediately after killing a boss
        public int bossMagnetTimer;
        public bool bossMagnet;

        public UIItemSlot SoulSlot;
        public override void Initialize()
        {
            PermanentBuffToggles = new bool[53]; //todo dont forget to increment this if you add buffs to the dictionary
            DamageDir = new Dictionary<int, float> {
                { 48, 4 }, //spike
                { 76, 4 }, //hellstone
                { 232, 4 } //wooden spike, in case tim decides to use them
            };

            SoulSlot = new UIItemSlot(Vector2.Zero, 52, ItemSlot.Context.InventoryItem, "Dark Souls", null, SoulSlotCondition, DrawSoulSlotBackground, null, null, false, true);
            SoulSlot.BackOpacity = 0.8f;
            SoulSlot.Item = new Item();
            SoulSlot.Item.SetDefaults(0, true);

            chestBankOpen = false;
            chestBank = -1;

            chestPiggyOpen = false;
            chestPiggy = -1;

        }

        public void DoPortableChest<T>(ref int whoAmI, ref bool toggle) where T : BonfireProjectiles, new() {
            int projectileType = ModContent.ProjectileType<T>();
            T instance = ModContent.GetInstance<T>();
            int bankID = instance.ChestType;
            LegacySoundStyle useSound = instance.UseSound;

            if (Main.projectile[whoAmI].active && Main.projectile[whoAmI].type == projectileType) {
                int oldChest = player.chest;
                player.chest = bankID;
                toggle = true;

                int num17 = (int)((player.position.X + player.width * 0.5) / 16.0);
                int num18 = (int)((player.position.Y + player.height * 0.5) / 16.0);
                player.chestX = (int)Main.projectile[whoAmI].Center.X / 16;
                player.chestY = (int)Main.projectile[whoAmI].Center.Y / 16;
                if ((oldChest != bankID && oldChest != -1) || num17 < player.chestX - Player.tileRangeX || num17 > player.chestX + Player.tileRangeX + 1 || num18 < player.chestY - Player.tileRangeY || num18 > player.chestY + Player.tileRangeY + 1) {
                    whoAmI = -1;
                    if (player.chest != -1) {
                        Main.PlaySound(useSound);
                    }

                    if (oldChest != bankID)
                        player.chest = oldChest;
                    else
                        player.chest = -1;

                    Recipe.FindRecipes();
                }
            }
            else {
                

                whoAmI = -1;
                player.chest = -1; //none
                Recipe.FindRecipes();
            }
        }

        public override void clientClone(ModPlayer clientClone) {
            tsorcRevampPlayer clone = clientClone as tsorcRevampPlayer;
            if (clone == null) { return; }

            clone.SoulSlot.Item = SoulSlot.Item.Clone();
        }

        public override void SendClientChanges(ModPlayer clientPlayer) {
            tsorcRevampPlayer oldClone = clientPlayer as tsorcRevampPlayer;
            if (oldClone == null) { return; }

            if (oldClone.SoulSlot.Item.IsNotTheSameAs(SoulSlot.Item)) {
                SendSingleItemPacket(1, SoulSlot.Item, -1, player.whoAmI);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)1);
            packet.Write((byte)player.whoAmI);
            ItemIO.Send(SoulSlot.Item, packet);
            packet.Send(toWho, fromWho);
        }

        internal void SendSingleItemPacket(int message, Item item, int toWho, int fromWho) {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)message);
            packet.Write((byte)player.whoAmI);
            ItemIO.Send(item, packet);
            packet.Send(toWho, fromWho);
        }

        public override TagCompound Save()
        {
            return new TagCompound 
            {
            {"warpX", warpX},
            {"warpY", warpY},
            {"warpWorld", warpWorld},
            {"warpSet", warpSet},
            {"townWarpX", townWarpX},
            {"townWarpY", townWarpY},
            {"townWarpWorld", townWarpWorld},
            {"townWarpSet", townWarpSet},
            {"gotPickaxe", gotPickaxe},
            {"FirstEncounter", FirstEncounter},
            {"ReceivedGift", ReceivedGift},


            {"soulSlot", ItemIO.Save(SoulSlot.Item) }
            };

        }

        public override void Load(TagCompound tag)
        {
            warpX = tag.GetInt("warpX");
            warpY = tag.GetInt("warpY");
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
            gotPickaxe = tag.GetBool("gotPickaxe");
            FirstEncounter = tag.GetBool("FirstEncounter");
            ReceivedGift = tag.GetBool("ReceivedGift");

            Item soulSlotSouls = ItemIO.Load(tag.GetCompound("soulSlot"));
            SoulSlot.Item = soulSlotSouls.Clone();


        }

        public override void ResetEffects() {
            SilverSerpentRing = false;
            DragonStone = false;
            SoulReaper = 5;
            DragoonBoots = false;
            player.eocDash = 0;
            player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;
            DuskCrownRing = false;
            DragoonBoots = false;
            GemBox = false;
            OldWeapon = false;
            Miakoda = false;
            RTQ2 = false;
            DarkInferno = false;
            BoneRevenge = false;
            SoulSiphon = false;
            CrimsonDrain = false;
            Shockwave = false;
            souldroplooptimer = 0;
            souldroptimer = 0;
            SOADrain = false;
            FracturingArmor = 1;
            MiakodaFull = false;
            MiakodaFullHeal1 = false;
            MiakodaCrescent = false;
            MiakodaCrescentDust1 = false;
            MiakodaNew = false;
            MiakodaNewDust1 = false;
            GiveBossZen = false;
            BossZenBuff = false;
            MagicWeapon = false;
            GreatMagicWeapon = false;
            CrystalMagicWeapon = false;
            DarkmoonCloak = false;
            manaShield = 0;
            ConditionOverload = false;
            supersonicLevel = 0;
            ConsSoulChanceMult = 0;
            SoulSickle = false;
    }

    public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {

            //This is going here, because unlike most hooks this one keeps running even when the game is paused via AutoPause
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>())
            {
                player.chest = -1;
            }

            if (!Main.gameMenu)
            {
                if (player.HasBuff(ModContent.BuffType<Chilled>()))
                {
                    r *= 0.3804f;
                    g *= 0.6902f;
                    b *= 254 / 255;
                }
                if (Shockwave)
                {
                    r *= 0.7372f;
                    g *= 0.5176f;
                    b *= 0.3686f;
                }
            }
        }

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            if (layers.Contains(PlayerLayer.HeldItem))
            {
                layers.Insert(layers.IndexOf(PlayerLayer.HeldItem) + 1, tsorcRevampGlowmasks);
            }
            layers.Add(tsorcRevampManaShield);
        }
        public override void ModifyDrawInfo(ref PlayerDrawInfo drawInfo)
        {
            base.ModifyDrawInfo(ref drawInfo);
        }

        public static readonly PlayerLayer tsorcRevampGlowmasks = new PlayerLayer("tsorcRevamp", "tsorcRevampGlowmasks", PlayerLayer.HeldItem, delegate (PlayerDrawInfo drawInfo)
        {

            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Item thisItem = modPlayer.player.HeldItem;

            #region Glaive Beam HeldItem glowmask and animation
            //If the player is holding the glaive beam
            if (thisItem.type == ModContent.ItemType<Items.Weapons.Ranged.GlaiveBeam>())
            {
                //And the projectile that creates the laser exists
                if (modPlayer.player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>()] > 0)
                {
                    Projectiles.GlaiveBeamLaser heldBeam;

                    //Then find the laser in the projectile array
                    for (int i = 0; i < Main.projectile.Length; i++)
                    {
                        //If it found it, we're in business.
                        if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>() && Main.projectile[i].owner == modPlayer.player.whoAmI)
                        {
                            //Get the transparent texture
                            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamHeldGlowmask];

                            //Get the animation frame
                            heldBeam = (Projectiles.GlaiveBeamLaser)Main.projectile[i].modProjectile;
                            int textureFrames = 10;
                            int frameHeight = (int)texture.Height / textureFrames;
                            int startY = frameHeight * (int)Math.Floor(9 * (heldBeam.Charge / 300));
                            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

                            //Get the offsets and shift the draw position based on them
                            Player drawPlayer = drawInfo.drawPlayer;
                            float textureMidpoint = texture.Height / (2 * textureFrames);
                            Vector2 drawPos = drawInfo.itemLocation - Main.screenPosition;
                            Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                            Vector2 originOffset = new Vector2(0, textureMidpoint);
                            ItemLoader.HoldoutOffset(drawPlayer.gravDir, drawPlayer.HeldItem.type, ref originOffset);
                            holdOffset.Y = originOffset.Y;
                            drawPos += holdOffset;

                            //Set the origin based on the offset point
                            Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                            //Shift everything if the player is facing the other way
                            if (drawPlayer.direction == -1)
                            {
                                origin.X = texture.Width + originOffset.X;
                            }

                            ///Draw, partner.
                            DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.player.HeldItem.scale, drawInfo.spriteEffects, 0);
                            Main.playerDrawData.Add(data);
                            break;
                        }
                    }
                }
            }
            #endregion
            //Make sure it's actually being displayed, not just selected
            if (modPlayer.player.itemAnimation > 0)
            {
                //Make sure it's from our mod
                if (thisItem.modItem != null && thisItem.modItem.mod == ModLoader.GetMod("tsorcRevamp"))               
                {
                    Texture2D texture = null;
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Pulsar>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PulsarGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.GWPulsar>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GWPulsarGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Polaris>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PolarisGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.ToxicCatalyzer>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ToxicCatalyzerGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.VirulentCatalyzer>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VirulentCatalyzerGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Biohazard>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
                    }

                    //If it's not on the list, don't bother.
                    if (texture != null)
                    {
                        #region animation
                        //These lines also can handle animation. Since this glowmask isn't animated a few of these lines are redundant, but they serve as an example for how it could be done.
                        //It's essentially the same to all other animation, you're just picking different parts of the texture to draw.
                        //In this case animationFrame is set as a function that depends on game time, making it animate as time passes. For another example, the Glaive Beam above animates based on weapon charge.
                        int textureFrames = 1;
                        int animationFrame = (int)Math.Floor(textureFrames * (((Main.time / 5) % 10) / 10));
                        int frameHeight = (int)texture.Height / textureFrames;
                        int startY = frameHeight * animationFrame;
                        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                        float textureMidpoint = texture.Height / (2 * textureFrames);
                        #endregion

                        //Since we're not doing animation, we can actually just 
                        //sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);

                        //Get the offsets and shift the draw position based on them
                        Player drawPlayer = drawInfo.drawPlayer;
                        Vector2 drawPos = drawInfo.itemLocation - Main.screenPosition;
                        Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                        Vector2 originOffset = new Vector2(0, textureMidpoint);
                        ItemLoader.HoldoutOffset(drawPlayer.gravDir, drawPlayer.HeldItem.type, ref originOffset);
                        holdOffset.Y = originOffset.Y;
                        drawPos += holdOffset;

                        //Set the origin based on the offset point
                        Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                        //Shift everything if the player is facing the other way
                        if (drawPlayer.direction == -1)
                        {
                            origin.X = texture.Width + originOffset.X;
                        }

                        DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.player.HeldItem.scale, drawInfo.spriteEffects, 3);
                        Main.playerDrawData.Add(data);
                    }
                }
            }                      
        });

        public static readonly PlayerLayer tsorcRevampManaShield = new PlayerLayer("tsorcRevamp", "tsorcRevampManaShield", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.manaShield > 0 && !modPlayer.player.dead)
            {
                if (modPlayer.player.statMana > Items.Accessories.ManaShield.manaCost)
                {
                    //If they didn't have enough mana for the shield last frame but do now, play a sound to let them know it's back up
                    if (!modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 28 is powerful magic cast
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 28);
                        modPlayer.shieldUp = true;
                    }

                    Lighting.AddLight(modPlayer.player.Center, 0f, 0.2f, 0.3f);

                    int shieldFrameCount = 8;
                    float shieldScale = 2.5f;

                    Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ManaShield];
                    Player drawPlayer = drawInfo.drawPlayer;
                    int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X);
                    int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y);
                    int frameHeight = texture.Height / shieldFrameCount;
                    int startY = frameHeight * (modPlayer.shieldFrame / 3);
                    Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                    Color newColor = Color.White;// Lighting.GetColor((int)((drawInfo.position.X + drawPlayer.width / 2f) / 16f), (int)((drawInfo.position.Y + drawPlayer.height / 2f) / 16f));
                    Vector2 origin = sourceRectangle.Size() / 2f;

                    DrawData data = new DrawData(texture, new Vector2(drawX, drawY), sourceRectangle, newColor, 0f, origin, shieldScale, SpriteEffects.None, 0);
                    Main.playerDrawData.Add(data);
                }
                else
                {
                    if (modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 60 is the Terra Beam
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 60);
                        modPlayer.shieldUp = false;
                    }
                    //If the player doesn't have enough mana to tank a hit, then draw particle effects to indicate their mana is too low for it to function.
                    int dust = Dust.NewDust(modPlayer.player.Center, 1, 1, 221, modPlayer.player.velocity.X + Main.rand.Next(-3, 3), modPlayer.player.velocity.Y + Main.rand.Next(-3, 3), 180, Color.Cyan, 1f);
                    Main.dust[dust].noGravity = true;
                    modPlayer.shieldUp = false;
                }
            }
            else
            {
                modPlayer.shieldUp = false;
            }
        });

        public override void PostUpdateEquips()
        {
            if (manaShield > 0)
            {
                player.manaRegenBuff = false;
            }
            int PTilePosX = (int)player.position.X / 16;
            bool Ocean = (PTilePosX < 750 || PTilePosX > Main.maxTilesX - 750);
            bool underground = (player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number

            if (((underground && player.ZoneHoly && !Ocean && !player.ZoneDungeon /*&& !player.ZoneOverworldHeight*/) || player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {

                player.gravControl = true;
            }


            for (int i = 0; i < 50; i++) {
                //block souls from going in normal inventory slots
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (player.inventory[i].type == ModContent.ItemType<DarkSoul>()) {
                    //if the player's soul slot is empty
                    if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoul>()) {
                        modPlayer.SoulSlot.Item = player.inventory[i].Clone();
                    }
                    else {
                        modPlayer.SoulSlot.Item.stack += player.inventory[i].stack;
                    }
                    //dont send the souls to the normal inventory
                    player.inventory[i].TurnToAir();
                }

            }


            if (Shockwave)
            {
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
                {
                    if (player.controlDown && player.velocity.Y != 0f)
                    {
                        player.gravity += 5f;
                        player.maxFallSpeed *= 1.25f;
                        if (!Falling)
                        {
                            fallStartY = player.position.Y;
                        }
                        if (player.velocity.Y > 12f)
                        {
                            Falling = true;
                            StopFalling = 0;
                            player.noKnockback = true;
                        }
                    }
                    if (player.velocity.Y == 0f && Falling && player.controlDown && !player.wet)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            int dustIndex2 = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 31, 0f, 0f, 100);
                            Main.dust[dustIndex2].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                            Main.dust[dustIndex2].fadeIn = 1.5f + Main.rand.Next(5) * 0.1f;
                            Main.dust[dustIndex2].noGravity = true;
                        }
                        FallDist = (int)((player.position.Y - fallStartY) / 16);
                        if (FallDist > 5)
                        {
                            Main.PlaySound(SoundID.Item, (int)player.Center.X, (int)player.Center.Y, 14);
                            for (int i = -9; i < 10; i++)
                            { //19 projectiles
                                Vector2 shotDirection = new Vector2(0f, -16f);
                                int shockwaveShot = Projectile.NewProjectile(player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * (Main.hardMode ? 2.6f : 2.4)), 12, player.whoAmI);
                                Main.projectile[shockwaveShot].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(0 - (10f * i))); // (180 / (projectilecount - 1))
                            }
                        }


                        Falling = false;
                    }
                    if (player.velocity.Y <= 2f)
                    {
                        StopFalling++;
                    }
                    else
                    {
                        StopFalling = 0;
                    }
                    if (StopFalling > 1)
                    {
                        Falling = false;
                    }
                }
                else
                {
                    var P = player;
                    if (Main.rand.Next(50) == 0)
                    {
                        int D = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D].noGravity = true;
                        Main.dust[D].velocity.X *= 1.2f;
                        Main.dust[D].velocity.X *= 1.2f;
                    }
                    if (Main.rand.Next(50) == 0)
                    {
                        int D2 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D2].noGravity = true;
                        Main.dust[D2].velocity.X *= -1.2f;
                        Main.dust[D2].velocity.X *= 1.2f;
                    }
                    if (Main.rand.Next(50) == 0)
                    {
                        int D3 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D3].noGravity = true;
                        Main.dust[D3].velocity.X *= 1.2f;
                        Main.dust[D3].velocity.X *= -1.2f;
                    }
                    if (Main.rand.Next(50) == 0)
                    {
                        int D4 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D4].noGravity = true;
                        Main.dust[D4].velocity.X *= -1.2f;
                        Main.dust[D4].velocity.X *= -1.2f;
                    }
                    int sw = (int)(Main.screenWidth);
                    int sh = (int)(Main.screenHeight);
                    int sx = (int)(Main.screenPosition.X);
                    int sy = (int)(Main.screenPosition.Y);
                    //bool wings = false;
                    //if (ModPlayer.HasItemInArmor(492) || ModPlayer.HasItemInArmor(493) || ModPlayer.HasItemInExtraSlots(492) || ModPlayer.HasItemInExtraSlots(493))
                    //{
                    //	wings = true;
                    //}
                    if (fallStart_old == -1) fallStart_old = P.fallStart;
                    int fall_dist = 0;
                    if (P.velocity.Y == 0f) // && !wings) // detect landing from a fall
                        fall_dist = (int)((float)((int)(P.position.Y / 16f) - fallStart_old) * P.gravDir);
                    Vector2 p_pos = P.position + new Vector2(P.width, P.height) / 2f;

                    if (fall_dist > 3) // just fell
                    {
                        for (int k = 0; k < Main.npc.Length; k++)
                        { // iterate through NPCs
                            NPC N = Main.npc[k];
                            if (!N.active || N.dontTakeDamage || N.friendly || N.life < 1) continue;
                            Vector2 n_pos = new Vector2(N.position.X + (float)N.width * 0.5f, N.position.Y + (float)N.height * 0.5f); // NPC location
                            int HitDir = -1;
                            if (n_pos.X > p_pos.X) HitDir = 1;
                            if ((N.position.X >= sx) && (N.position.X <= sx + sw) && (N.position.Y >= sy) && (N.position.Y <= sy + sh))
                            { // on screen
                                N.StrikeNPC(2 * fall_dist, 5f, HitDir);
                                if (Main.netMode != NetmodeID.SinglePlayer) NetMessage.SendData(MessageID.StrikeNPC, -1, -1, null, k, 2 * fall_dist, 10f, HitDir, 0); // for multiplayer support
                                                                                                                                                                      // optionally add debuff here
                            } // END on screen
                        } // END iterate through NPCs
                    } // END just fell
                    fallStart_old = P.fallStart;
                }
            }
            if (!Shockwave)
            {
                Falling = false;
            }

            if (CrimsonDrain)
            {
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
                {
                    for (int l = 0; l < 200; l++)
                    {
                        NPC nPC = Main.npc[l];
                        if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(player.Center, nPC.Center) <= 200)
                        {
                            nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 2);
                        }
                    }

                    Vector2 centerOffset = new Vector2(player.Center.X + 2 - player.width / 2, player.Center.Y + 6 - player.height / 2);
                    for (int j = 1; j < 30; j++)
                    {
                        var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 8 == 0 ? Main.rand.Next(15, 125) : 125)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 235, player.velocity.X, player.velocity.Y);
                        Main.dust[x].noGravity = true;
                    }
                }
                else
                { //old crimson pot
                    var P = player;
                    int x = (int)P.position.X;
                    int y = (int)P.position.Y;
                    for (int k = 0; k < Main.npc.Length; k++)
                    {
                        NPC N = Main.npc[k];
                        if (N.townNPC) continue;
                        if (!N.active || N.dontTakeDamage || N.friendly || N.life < 1) continue;
                        if (N.position.X >= x - 320 && N.position.X <= x + 320 && N.position.Y >= y - 320 && N.position.Y <= y + 320)
                        {
                            count++;
                            if (count % 50 == 0)
                            {
                                foreach (NPC N2 in Main.npc)
                                {
                                    if (N2.position.X >= x - 320 && N2.position.X <= x + 320 && N2.position.Y >= y - 320 && N2.position.Y <= y + 320)
                                    {
                                        if (!N2.active || N2.dontTakeDamage || N2.townNPC || N2.life < 1 || N2.boss || N2.realLife >= 0) continue;
                                        N2.StrikeNPC(1, 0f, 1);
                                    }
                                }
                                count = 0;
                            }
                        }
                    }
                }
            }


            #region Soul Siphon Dusts


            if (SoulSiphon)
            {

                if (Main.rand.Next(3) == 0) //outermost "ring"
                {
                    int num5 = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[num5].noGravity = true;
                    Main.dust[num5].velocity *= 0.75f;
                    Main.dust[num5].fadeIn = 1.5f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                    Main.dust[num5].velocity = vector;
                    vector.Normalize();
                    vector *= Main.rand.Next(220, 900);
                    Main.dust[num5].position = player.Center - vector;
                }

                if (Main.rand.Next(6) == 0)
                {
                    int x = Dust.NewDust(player.position, player.width, player.height, 89, player.velocity.X, player.velocity.Y, 120, default(Color), 1f);
                    Main.dust[x].noGravity = true;
                    Main.dust[x].velocity *= 0.75f;
                    Main.dust[x].fadeIn = 1.3f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.05f; //velocity towards player
                    Main.dust[x].velocity = vector;
                    vector.Normalize();
                    vector *= 200f; //spawn distance from player
                    Main.dust[x].position = player.Center - vector;

                    //Vector2.Normalize(start - end) * someSpeed //start and end are also Vector2 // Aparently another way to make things move toward each other

                }

                if (Main.rand.Next(3) == 0)
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.052f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 150f;
                    Main.dust[z].position = player.Center - vectorother;
                }

                if (Main.rand.Next(2) == 0)
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 90f;
                    Main.dust[z].position = player.Center - vectorother;
                }

                if (Main.rand.Next(2) == 0) //innermost "ring"
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 45f;
                    Main.dust[z].position = player.Center - vectorother;
                }
            }

            #endregion
            #region consistent hellstone and spike damage
            float REDUCE = CheckReduceDefense(player.position, player.width, player.height, player.fireWalk); // <--- added firewalk parameter
            if (REDUCE != 0)
            {
                REDUCE = 1f - REDUCE;
                player.statDefense = (int)(player.statDefense * REDUCE);
            }
            #endregion
            #region boss zen
            GiveBossZen = CheckBossZen();
            if (GiveBossZen && ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig)
            {
                player.AddBuff(ModContent.BuffType<BossZenBuff>(), 2, false);
            }
            #endregion
            #region boss magnet
            //actual item grab range is in GlobalItem::GrabRange
            if (bossMagnet)
            {
                bossMagnetTimer--;
            }
            if (bossMagnetTimer == 0)
            {
                bossMagnet = false;
            }
            #endregion




        }

        public override void PostUpdateRunSpeeds()
        {
            if (supersonicLevel == 0)
            {
                return;
            }
            else
            {
                float moveSpeedPercentBoost = 1;
                float baseSpeed = 1;

                //SupersonicBoots
                if (supersonicLevel == 1)
                {
                    //moveSpeedPercentBoost is what percent of a player's moveSpeed bonus should be applied to their max running speed
                    //For vanilla hermes boots and their upgrades, this is 0
                    moveSpeedPercentBoost = 0.35f;
                    //6f is hermes boots speed.
                    baseSpeed = 6f;
                    player.moveSpeed += 0.2f;
                }
                //SupersonicWings
                if (supersonicLevel == 2)
                {
                    moveSpeedPercentBoost = 0.5f;
                    baseSpeed = 6.8f;
                    player.moveSpeed += 0.3f;
                }
                //SupersonicWings2
                if (supersonicLevel == 3)
                {

                    moveSpeedPercentBoost = 1f;
                    baseSpeed = 7.5f;
                    player.moveSpeed += 0.6f;
                }


                //((player.moveSpeed * 0.5f) + 0.5) means 50% of the player's moveSpeed bonus will be applied
                //The general form is ((player.moveSpeed * %theyshouldget) + (1 - %theyshouldget))
                player.accRunSpeed = baseSpeed * ((player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));
                player.maxRunSpeed = baseSpeed * ((player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));
            }
        }

        public static bool CheckBossZen()
        {
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss)
                {
                    return true;
                }
            }
            return false;
        }

        public static float CheckReduceDefense(Vector2 Position, int Width, int Height, bool fireWalk)
        {

            int playerTileXLeft = (int)(Position.X / 16f) - 1;
            int playerTileXRight = (int)((Position.X + Width) / 16f) + 2;
            int playerTileYBottom = (int)(Position.Y / 16f) - 1;
            int playerTileYTop = (int)((Position.Y + Height) / 16f) + 2;

            #region sanity
            if (playerTileXLeft < 0)
            {
                playerTileXLeft = 0;
            }
            if (playerTileXRight > Main.maxTilesX)
            {
                playerTileXRight = Main.maxTilesX;
            }
            if (playerTileYBottom < 0)
            {
                playerTileYBottom = 0;
            }
            if (playerTileYTop > Main.maxTilesY)
            {
                playerTileYTop = Main.maxTilesY;
            }
            #endregion

            for (int i = playerTileXLeft; i < playerTileXRight; i++)
            {
                for (int j = playerTileYBottom; j < playerTileYTop; j++)
                {
                    if (Main.tile[i, j] != null && Main.tile[i, j].active())
                    {
                        Vector2 TilePos;
                        TilePos.X = i * 16;
                        TilePos.Y = j * 16;

                        int type = Main.tile[i, j].type;

                        if (DamageDir.ContainsKey(type) && !(fireWalk && type == 76))
                        {
                            float a = DamageDir[type];
                            float z = 0.5f;
                            if (Position.X + Width > TilePos.X - z &&
                                Position.X < TilePos.X + 16f + z &&
                                Position.Y + Height > TilePos.Y - z &&
                                Position.Y < TilePos.Y + 16f + z)
                            {
                                return a;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public static float CheckSoulsMultiplier(Player player)
        {
            float multiplier = 1f;
            if (player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing)
            {
                multiplier += 0.25f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
            {
                multiplier += 0.2f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain)
            {
                multiplier += 0.4f;
            }
            return multiplier;
        }


        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            }
            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }

        public override void OnHitAnything(float x, float y, Entity victim) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            }
            base.OnHitAnything(x, y, victim);
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().DeleteDroppedSoulsOnDeath)
            {
                for (int i = 0; i < 400; i++)
                {
                    if (Main.item[i].type == ModContent.ItemType<DarkSoul>())
                    {
                        Main.item[i].active = false;
                    }
                }
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            Projectile.NewProjectile(player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, player.whoAmI);
            Main.PlaySound(SoundID.NPCDeath58.WithVolume(0.8f).WithPitchVariance(.3f), player.position);
        }
        public override void UpdateDead()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath)
            {
                souldroptimer++;
                if (souldroptimer == 5 && souldroplooptimer < 13)
                {
                    foreach (Item item in player.inventory)
                    { //leaving this in case someone decides to move souls to their normal inventory to stop them from being dropped on death :)
                        if (item.type == ModContent.ItemType<DarkSoul>() /*&& Main.netMode != NetmodeID.MultiplayerClient*/)
                        { //could this be dropping double though? Test with Zeo
                            Item.NewItem(player.Center, item.type, item.stack);
                            souldroplooptimer++;
                            souldroptimer = 0;
                            item.stack = 0;
                        }
                    }

                    if (SoulSlot.Item.stack > 0) {
                        if (souldroplooptimer == 12) {
                            Item.NewItem(player.Center, SoulSlot.Item.type, SoulSlot.Item.stack);
                            SoulSlot.Item.TurnToAir();
                        }
                        else {
                            Item.NewItem(player.Center, SoulSlot.Item.type, 0);
                        }
                        souldroplooptimer++;
                        souldroptimer = 0;
                    }
                }
            }
            DarkInferno = false;
            Falling = false;
            FracturingArmor = 1;
        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
        {
            if (MeleeArmorVamp10)
            {
                if (Main.rand.Next(10) == 0)
                {
                    player.HealEffect(damage / 10);
                    player.statLife += (damage / 10);

                }
            }
            if (NUVamp)
            {
                if (Main.rand.Next(5) == 0)
                {
                    player.HealEffect(damage / 4);
                    player.statLife += (damage / 4);
                }
            }
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
                    {

                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;

                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2)
                        {
                            player.statLife = player.statLifeMax2;
                        }

                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent)
            { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew)
            { //Miakoda New Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Main.PlaySound(SoundID.Item81.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, int damage, float knockback, bool crit)
        {
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;



                        //2 per 100 max hp, plus 2
                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2)
                        {
                            player.statLife = player.statLifeMax2;
                        }

                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent)
            { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew)
            { //Miakoda New Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Main.PlaySound(SoundID.Item81.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }
        }

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            if (OldWeapon)
            {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (OldWeapon)
            {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
            if (((proj.type == ProjectileID.MoonlordArrow) || (proj.type == ProjectileID.MoonlordArrowTrail)) && player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>())
            {
                damage = (int)(damage * 0.55);
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit)
        {
            int NT = npc.type;
            if (DragonStone)
            {
                if (NT == 2 || NT == 6 || NT == 34 || NT == 42 || NT == 48 || NT == 49 || NT == 51 || NT == 60 || NT == 61 || NT == 62 || NT == 66 || NT == 75 || NT == 87 || NT == 88 || NT == 89 || NT == 90 || NT == 91 || NT == 92 || NT == 93 || NT == 94 || NT == 112 || NT == 122 || NT == 133 || NT == 137
                    || NT == NPCID.Probe
                    || NT == NPCID.IceBat
                    || NT == NPCID.Lavabat
                    || NT == NPCID.GiantFlyingFox
                    || NT == NPCID.RedDevil
                    || NT == NPCID.VampireBat
                    || NT == NPCID.IceElemental
                    || NT == NPCID.PigronCorruption
                    || NT == NPCID.PigronHallow
                    || NT == NPCID.PigronCrimson
                    || NT == NPCID.Crimera
                    || NT == NPCID.MossHornet
                    || NT == NPCID.CrimsonAxe
                    || NT == NPCID.FloatyGross
                    || NT == NPCID.Moth
                    || NT == NPCID.Bee
                    || NT == NPCID.FlyingFish
                    || NT == NPCID.FlyingSnake
                    || NT == NPCID.AngryNimbus
                    || NT == NPCID.Parrot
                    || NT == NPCID.Reaper
                    || NT == NPCID.IchorSticker
                    || NT == NPCID.DungeonSpirit
                    || NT == NPCID.Ghost
                    || NT == NPCID.ElfCopter
                    || NT == NPCID.Flocko
                    || NT == NPCID.MartianDrone
                    || NT == NPCID.MartianProbe
                    || NT == NPCID.ShadowFlameApparition
                    || NT == NPCID.MothronSpawn
                    || NT == NPCID.GraniteFlyer
                    || NT == NPCID.FlyingAntlion
                    || NT == NPCID.DesertDjinn
                    || NT == NPCID.SandElemental)
                {
                    damage = 0;
                }
            }
            if (UndeadTalisman)
            {
                if (NPCID.Sets.Skeletons.Contains(npc.type)
                    || npc.type == NPCID.Zombie
                    || npc.type == NPCID.Skeleton
                    || npc.type == NPCID.BaldZombie
                    || npc.type == NPCID.AngryBones
                    || npc.type == NPCID.ArmoredViking
                    || npc.type == NPCID.UndeadViking
                    || npc.type == NPCID.DarkCaster
                    || npc.type == NPCID.CursedSkull
                    || npc.type == NPCID.UndeadMiner
                    || npc.type == NPCID.Tim
                    || npc.type == NPCID.DoctorBones
                    || npc.type == NPCID.ArmoredSkeleton
                    || npc.type == NPCID.Mummy
                    || npc.type == NPCID.DarkMummy
                    || npc.type == NPCID.LightMummy
                    || npc.type == NPCID.Wraith
                    || npc.type == NPCID.SkeletonArcher
                    || npc.type == NPCID.PossessedArmor
                    || npc.type == NPCID.TheGroom
                    || npc.type == NPCID.SkeletronHand
                    || npc.type == NPCID.SkeletronHead
                    /* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/)
                {
                    damage -= 15;

                    if (damage < 0) damage = 0;
                }
            }

        }

        public override void OnHitByNPC(NPC npc, int damage, bool crit)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 7f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 80, 9f, 0, 0, 0);
                }
            }
            if (npc.type == NPCID.SkeletronPrime && Main.rand.Next(2) == 0)
            {
                player.AddBuff(BuffID.Bleeding, 1800);
                player.AddBuff(BuffID.OnFire, 600);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, bool crit)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 6f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 60, 8f, 0, 0, 0);
                }
            }
            if (projectile.type == ProjectileID.DeathLaser && Main.rand.Next(2) == 0)
            {
                player.AddBuff(BuffID.BrokenArmor, 180);
                player.AddBuff(BuffID.OnFire, 180);
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref int damage, ref bool crit)
        {
            if (UndeadTalisman)
            {
                if (proj.type == ProjectileID.SkeletonBone || proj.type == ProjectileID.Skull)
                {
                    if (!Main.expertMode)
                    {
                        damage -= 8;
                    }
                    if (Main.expertMode)
                    {
                        damage -= 4;
                    }

                    if (damage < 0) damage = 0;
                }
            }
        }

        public override void UpdateBadLifeRegen()
        {
            if (DarkInferno)
            {
                if (player.lifeRegen > 0)
                {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -11;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(player.position, player.width / 2, player.height / 2, 54, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(player.position, player.width / 2, player.height / 2, 58, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (SOADrain)
            {
                if (player.lifeRegen > 0)
                {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -15;
                if (Main.rand.Next(3) == 0)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 235, player.velocity.X, player.velocity.Y, 140, default, 0.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed)
            {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
        }

        public override void PreUpdate()
        {
            //Main.NewText(darkSoulQuantity);

            darkSoulQuantity = player.CountItem(ModContent.ItemType<DarkSoul>(), 999999);

            //the item in the soul slot will only ever be souls, so we dont need to check type
            if (SoulSlot.Item.stack > 0) { darkSoulQuantity += SoulSlot.Item.stack; }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                tsorcScriptedEvents.PlayerScriptedEventCheck(this.player);
            }
            MiakodaEffectsTimer++;

            if (DragoonBoots && DragoonBootsEnable)
            { //lets do this the smart way
                Player.jumpSpeed += 10f;

            }

            if (!player.HasBuff(ModContent.BuffType<Bonfire>()))
            { //this ensures that BonfireUIState is only visible when within Bonfire range
                BonfireUIState.Visible = false;
            }

            if (MiakodaFullHeal1)
            { //dust loop on player the instant they get healed
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 107, 0f, 0f, 30, default(Color), .75f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (MiakodaCrescentDust1)
            { //dust loop on player the instant they get imbue
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 164, 0f, 0f, 30, default(Color), 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 5f);
                    Main.dust[dust].noGravity = false;
                }
            }
            if (MiakodaCrescentBoost)
            {
                MiakodaCrescentBoostTimer++;
            }
            if (MiakodaCrescentBoostTimer > 150)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = false;
                MiakodaCrescentBoostTimer = 0;
            }

            if (MiakodaNewDust1)
            { //dust loop on player the instant they get boost
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0f, 50, default(Color), 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 7.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (MiakodaNewBoost)
            {
                MiakodaNewBoostTimer++;
                player.armorEffectDrawShadow = true;

            }
            if (MiakodaNewBoostTimer > 150)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = false;
                MiakodaNewBoostTimer = 0;
            }



            #region manashield
            if (manaShield > 0)
            {
                shieldFrame++;
                if (shieldFrame > 23)
                {
                    shieldFrame = 0;
                }

                //Disable Mana Regen Potions
                player.manaRegenBuff = false;
                player.buffImmune[BuffID.ManaRegeneration] = true;
            }
            #endregion manashield




            #region Abyss Shader
            bool hasCoA = false;

            if (Main.netMode != NetmodeID.Server) {

                //does the player have a covenant of artorias
                for (int i = 3; i < (8 + player.extraAccessorySlots); i++) {
                    if (player.armor[i].type == ModContent.ItemType<Items.Accessories.CovenantOfArtorias>()) {
                        hasCoA = true;
                        break;
                    }
                }

                //if they do, and the shader is inactive
                if (hasCoA && !(Filters.Scene["tsorcRevamp:TheAbyss"].Active)) {
                    Filters.Scene.Activate("tsorcRevamp:TheAbyss");
                }

                //if the abyss shader is active and the player is no longer wearing the CoA
                if (Filters.Scene["tsorcRevamp:TheAbyss"].Active && !hasCoA) {
                    Filters.Scene["tsorcRevamp:TheAbyss"].Deactivate();
                }
            }

            #endregion
        }



        //On hit, subtract the mana cost and disable natural mana regen for a short period
        //The latter is absolutely necessary, because natural mana regen scales with your base mana
        //Even as melee there are mana boosting accessories you can stack, as well as armor like Dragoon that makes mana regen obscenely powerful.
        //This means you can tank until your mana bar is exhausted, then have to back off for a bit and actually dodge
        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {
            base.Hurt(pvp, quiet, damage, hitDirection, crit);
            if (manaShield == 1)
            {
                if (player.statMana >= Items.Accessories.ManaShield.manaCost)
                {
                    player.statMana -= Items.Accessories.ManaShield.manaCost;
                    player.manaRegenDelay = Items.Accessories.ManaShield.regenDelay;
                }
            }
        }

        //Reduces the mana restored from potions and such to zero
        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            if (manaShield >= 1)
            {
                healValue = 0;
            }
        }

        public override void PreUpdateBuffs() {
            if (chestBank >= 0) {
                DoPortableChest<SafeProjectile>(ref chestBank, ref chestBankOpen);
            }
            if (chestPiggy >= 0) {
                DoPortableChest<PiggyBankProjectile>(ref chestPiggy, ref chestPiggyOpen);
            }

            if (!Main.playerInventory) {
                chestPiggy = -1;
                chestPiggyOpen = false;
                chestBank = -1;
                chestBankOpen = false;
            }
        }

        public override void PostUpdateBuffs()
        {
            if (MiakodaCrescentBoost)
            {
                player.allDamageMult += 0.07f;
            }

            if (MiakodaNewBoost)
            {
                player.moveSpeed += 0.9f;
                player.endurance = .5f;
                player.noKnockback = true;
            }
        }

        public override void FrameEffects()
        {
            if (MiakodaNewBoost)
            {
                player.armorEffectDrawShadow = true;
            }
        }

        public override void OnEnterWorld(Player player)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !gotPickaxe)
            { //sandbox mode only, and only once
                player.QuickSpawnItem(ModContent.ItemType<DiamondPickaxe>());
                gotPickaxe = true;
            }
            if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.EmeraldHerald>()))
            {
                NPC.NewNPC((int)player.position.X + 3000, (int)player.position.Y, ModContent.NPCType<NPCs.Friendly.EmeraldHerald>());
            }
        }

        public override void OnRespawn(Player player)
        {
            tsorcScriptedEvents.RefreshEvents();
        }

        public void Draw(SpriteBatch spriteBatch) {
            if (!ShouldDrawSoulSlot()) {
                return;
            }

            float origScale = Main.inventoryScale;
            Main.inventoryScale = 0.85f;

            int slotIndexX = 11;
            int slotIndexY = 0;
            int slotPosX = (int)(20f + (float)(slotIndexX * 56) * Main.inventoryScale);
            int slotPosY = (int)(20f + (float)(slotIndexY * 56) * Main.inventoryScale) + 18;

            SoulSlot.Position = new Vector2(slotPosX, slotPosY);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, "Souls", new Vector2(slotPosX + 6f, slotPosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0f);

            SoulSlot.Draw(spriteBatch);

            Main.inventoryScale = origScale;

            SoulSlot.Update();

        }


        #region Soul Slot
        internal static bool SoulSlotCondition(Item item) {
            if (item.type != ModContent.ItemType<DarkSoul>()) {
                return false;
            }
            return true;
        }

        internal void DrawSoulSlotBackground(UIObject sender, SpriteBatch spriteBatch) {
            UIItemSlot slot = (UIItemSlot)sender;

            if (ShouldDrawSoulSlot()) {
                slot.OnDrawBackground(spriteBatch);

                if (slot.Item.stack == 0) {
                    Texture2D tex = mod.GetTexture("UI/SoulSlotBackground");
                    Vector2 origin = tex.Size() / 2f * Main.inventoryScale;
                    Vector2 position = slot.Rectangle.TopLeft();

                    spriteBatch.Draw(
                        tex,
                        position + (slot.Rectangle.Size() / 2f) - (origin / 2f),
                        null,
                        Color.White * 0.35f,
                        0f,
                        origin,
                        Main.inventoryScale,
                        SpriteEffects.None,
                        0f); // layer depth 0 = front
                }
            }
        }

        internal static bool ShouldDrawSoulSlot() {
            return (Main.playerInventory);
        }


        #endregion
    }
}
