using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace tsorcRevamp.Tiles
{

    public class SoapstoneTile : ModTile
    {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Origin = new(0, 0);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<SoapstoneTileEntity>().Hook_AfterPlacement, -1, 0, false);
            TileObjectData.addTile(Type);
            LocalizedText name = CreateMapEntryName();
            // name.SetDefault("Soapstone Message");
            AddMapEntry(new Color(100, 0, 20), name);
            DustType = 117;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileLighted[Type] = false;
            Main.tileSpelunker[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = false;
            Main.tileSolid[Type] = false;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return false;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            fail = true;
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (TileUtils.TryGetTileEntityAs(i, j, out SoapstoneTileEntity entity))
            {
                Vector2 zero = new(Main.offScreenRange, Main.offScreenRange);
                if (Main.drawToScreen)
                {
                    zero = Vector2.Zero;
                }

                TransparentTextureHandler.TransparentTextureType type = TransparentTextureHandler.TransparentTextureType.SoapstoneMessage;
                Texture2D texture = TransparentTextureHandler.TransparentTextures[type];

                Vector2 textureSize = texture.Size();

                Vector2 position = new(i * 16 - ((int)Main.screenPosition.X + textureSize.X / 2) + 16, j * 16 - (int)Main.screenPosition.Y);
                Vector2 worldPosition = new Vector2(i + 1, j + 1) * 16;
                float distance = Vector2.Distance(Main.LocalPlayer.Center, worldPosition);
                float mouseDistance = Vector2.Distance(tsorcRevampPlayer.RealMouseWorld, worldPosition);

                bool mouseLineOfSight = (Collision.CanHitLine(Main.LocalPlayer.Center, 1, 1, worldPosition, 1, 1));
                if (entity.read)
                {
                    mouseLineOfSight = true;
                }

                bool selectedSoapstone = false;
                bool mouseInRange = mouseDistance < 240 && mouseLineOfSight;
                bool playerInRange = distance <= 40;

                tsorcRevampConfig config = ModContent.GetInstance<tsorcRevampConfig>();
                bool categoryDisabled = SoapstoneTileEntity.IsEntirelyDisabled(entity, config);
                bool autoOpen = config.AutoOpenSoapstones;

                // Location banner: fire when player walks near a location-tagged sign whose
                // ID differs from the last one shown. Independent of category-disable / show-on-click.
                if (playerInRange
                    && !config.DisableLocationBanner
                    && !string.IsNullOrEmpty(entity.locationName)
                    && tsorcRevamp.LastShownLocationId != entity.ID)
                {
                    tsorcRevamp.LastShownLocationId = entity.ID;
                    tsorcRevamp.LocationBannerText = entity.locationName.ToUpperInvariant();
                    tsorcRevamp.LocationBannerTimer = tsorcRevamp.LOCATION_BANNER_TOTAL;
                }

                // If the bubble is sticky-open (user clicked Show), check whether the mouse has
                // moved far enough from the click position to dismiss it.
                if (entity.manuallyOpened
                    && Vector2.Distance(Main.MouseScreen, entity.clickedAtMouse) > SoapstoneTileEntity.MANUAL_DISMISS_DISTANCE)
                {
                    entity.manuallyOpened = false;
                }

                bool keepOpen = autoOpen || entity.manuallyOpened;

                //Main.NewText("soap " + tsorcRevamp.NearbySoapstoneMouse);
                //If the mouse is already nearby a soapstone
                if (!categoryDisabled && tsorcRevamp.NearbySoapstoneMouse)
                {
                    //If the mouse is closer to this soapstone than to the previous one, set this as the current soapstone
                    if (mouseDistance < tsorcRevamp.NearbySoapstoneMouseDistance && mouseInRange && distance < 600)
                    {
                        tsorcRevamp.NearbySoapstone = entity;
                        if (!entity.hidden)
                        {
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 30);
                        }

                        tsorcRevamp.NearbySoapstoneMouse = true;
                        tsorcRevamp.NearbySoapstoneMouseDistance = mouseDistance;
                        if (keepOpen) entity.timer = 25;
                        // Only auto-mark as read on proximity if the bubble actually opens.
                        // When AutoOpen is off, read is set on click (see Systems display).
                        if (autoOpen) entity.read = true;
                        entity.nearPlayer = true;
                    }
                }
                else if (!categoryDisabled && (playerInRange || (mouseInRange && distance < 240)))
                {
                    tsorcRevamp.NearbySoapstone = entity;
                    if (!entity.hidden)
                    {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 30);
                    }

                    if (mouseInRange)
                    {
                        tsorcRevamp.NearbySoapstoneMouse = true;
                        tsorcRevamp.NearbySoapstoneMouseDistance = mouseDistance;
                    }
                    if (keepOpen) entity.timer = 25;
                    if (autoOpen) entity.read = true;
                    entity.nearPlayer = true;
                }


                if (!selectedSoapstone)
                {
                    if (entity.timer > 0)
                    {
                        entity.timer -= 5;

                        if (entity.timer <= 0)
                        {
                            entity.timer = 0;
                            entity.nearPlayer = false;
                            entity.manuallyOpened = false;

                            if (tsorcRevamp.NearbySoapstone == entity)
                            {
                                tsorcRevamp.NearbySoapstone = null;
                            }

                            if (ModContent.GetInstance<tsorcRevampConfig>().HideSoapstones)
                            {
                                entity.hidden = true;
                            }
                        }
                    }
                }


                Color ShimmerColor = Color.PapayaWhip;

                if (entity.read)
                {
                    ShimmerColor.R /= 4;
                    ShimmerColor.G /= 4;
                    ShimmerColor.B /= 3;
                }

                spriteBatch.Draw(texture, position + zero, new Rectangle(0, 0, (int)textureSize.X, (int)textureSize.Y), ShimmerColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }
    }
    public class SoapstoneTileEntity : ModTileEntity
    {
        public string text;
        public int textWidth;
        public SoapstoneStyle style;
        public float timer;
        public bool nearPlayer;
        public bool read = false;
        public bool hidden = false;
        // Comma-separated category tags (story, lore, hint, tutorial, location). Null/empty => hint.
        public string category;
        // Display name for the location banner. Null/empty => no banner.
        public string locationName;
        // Transient UI state (not saved, not synced): true while the player has clicked "Show"
        // and the bubble is sticky. Reset to false when the player dismisses it (mouse moved
        // far from click point, or walked out of range).
        public bool manuallyOpened;
        // Screen-space mouse position when the Show button was clicked. Used to detect dismissal.
        public Vector2 clickedAtMouse;
        // Distance the mouse must travel from clickedAtMouse to dismiss the sticky bubble.
        public const float MANUAL_DISMISS_DISTANCE = 220f;

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);

                //Sync the placement of the tile entity with other clients
                //The "type" parameter refers to the tile type which placed the tile entity, so "Type" (the type of the tile entity) needs to be used here instead
                NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);
            }

            //ModTileEntity.Place() handles checking if the entity can be placed, then places it for you
            int placedEntity = Place(i, j);
            return placedEntity;
        }


        public override bool IsTileValidForEntity(int x, int y)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            return tile.HasTile && tile.TileType == ModContent.TileType<SoapstoneTile>();
        }

        public override void OnNetPlace()
        {
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }

        public override void Update()
        {

            if (text == null)
            {
                foreach (SoapstoneMessage cache in SoapstoneMessage.SoapstoneList)
                {
                    if (Position == cache.location)
                    {
                        text = cache.text;
                        break;
                    }
                }
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(text);
            writer.Write(textWidth);
            writer.Write(read);
            writer.Write(category ?? string.Empty);
            writer.Write(locationName ?? string.Empty);
        }

        public override void NetReceive(BinaryReader reader)
        {
            text = reader.ReadString();
            textWidth = reader.ReadInt32();
            read = reader.ReadBoolean();
            string cat = reader.ReadString();
            category = string.IsNullOrEmpty(cat) ? null : cat;
            string loc = reader.ReadString();
            locationName = string.IsNullOrEmpty(loc) ? null : loc;
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Add("text", text);
            tag.Add("boxWidth", textWidth);
            tag.Add("read", read);
            if (!string.IsNullOrEmpty(category)) tag.Add("category", category);
            if (!string.IsNullOrEmpty(locationName)) tag.Add("locationName", locationName);
        }

        public override void LoadData(TagCompound tag)
        {
            string? saved = tag.GetString("text");
            text = saved;

            int? width = tag.GetInt("boxWidth");
            textWidth = width ?? SoapstoneMessage.DEFAULT_WIDTH;

            bool? dim = tag.GetBool("read");
            read = dim ?? false;

            // category and locationName are re-applied from JSON each world load via BuildSoapstones,
            // but we persist them so they survive even if the JSON file is missing later.
            category = tag.ContainsKey("category") ? tag.GetString("category") : null;
            locationName = tag.ContainsKey("locationName") ? tag.GetString("locationName") : null;
        }

        public static bool HasCategory(string categoryField, string tag)
        {
            if (string.IsNullOrEmpty(categoryField))
                return tag == "hint"; // default
            foreach (string part in categoryField.Split(','))
            {
                if (part.Trim().Equals(tag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // True iff every category on this entity is currently disabled by config.
        // Hint and location are never disable-able; presence of either is enough to keep the sign live.
        public static bool IsEntirelyDisabled(SoapstoneTileEntity entity, tsorcRevampConfig config)
        {
            string cat = entity.category;
            // No category -> implicit hint -> never disabled.
            if (string.IsNullOrEmpty(cat)) return false;

            bool anyAllowed = false;
            foreach (string part in cat.Split(','))
            {
                string t = part.Trim().ToLowerInvariant();
                bool disabled = t switch
                {
                    "story" => config.DisableStorySoapstones,
                    "lore" => config.DisableLoreSoapstones,
                    "tutorial" => config.DisableTutorialSoapstones,
                    "hint" => false,
                    "location" => false,
                    _ => false, // unknown tag -> treat as allowed
                };
                if (!disabled) { anyAllowed = true; break; }
            }
            return !anyAllowed;
        }

        // Returns the localized "Show X & Y" label for the click-to-open button,
        // built from the entity's category tags.
        public static string BuildShowButtonText(SoapstoneTileEntity entity)
        {
            string cat = entity.category;
            System.Collections.Generic.List<string> parts = new();
            if (string.IsNullOrEmpty(cat))
            {
                parts.Add(Utilities.LangUtils.GetTextValue("UI.TagHint"));
            }
            else
            {
                foreach (string raw in cat.Split(','))
                {
                    string t = raw.Trim().ToLowerInvariant();
                    string display = t switch
                    {
                        "story" => Utilities.LangUtils.GetTextValue("UI.TagStory"),
                        "lore" => Utilities.LangUtils.GetTextValue("UI.TagLore"),
                        "hint" => Utilities.LangUtils.GetTextValue("UI.TagHint"),
                        "tutorial" => Utilities.LangUtils.GetTextValue("UI.TagTutorial"),
                        "location" => Utilities.LangUtils.GetTextValue("UI.TagLocation"),
                        _ => null,
                    };
                    if (display != null && !parts.Contains(display)) parts.Add(display);
                }
                if (parts.Count == 0)
                    parts.Add(Utilities.LangUtils.GetTextValue("UI.TagHint"));
            }

            string joined;
            if (parts.Count == 1) joined = parts[0];
            else if (parts.Count == 2) joined = parts[0] + " & " + parts[1];
            else joined = string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " & " + parts[parts.Count - 1];

            return " " + Utilities.LangUtils.GetTextValue("UI.ShowPrefix") + " " + joined + "  ";
        }
    }
}
