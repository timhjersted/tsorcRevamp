using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    public class MNPC : GlobalNPC
    {
        public static Random TempSyncedRand = new Random(0);

        public static float RotationTo(Vector2 startPos, Vector2 endPos)
        {
            return (float)Math.Atan2(endPos.Y - startPos.Y, endPos.X - startPos.X);
        }

        public static Vector2 RotateVector(Vector2 origin, Vector2 vecToRot, float rot)
        {
            float newPosX = (float)(Math.Cos(rot) * (vecToRot.X - origin.X) - Math.Sin(rot) * (vecToRot.Y - origin.Y) + origin.X);
            float newPosY = (float)(Math.Sin(rot) * (vecToRot.X - origin.X) + Math.Cos(rot) * (vecToRot.Y - origin.Y) + origin.Y);
            return new Vector2(newPosX, newPosY);
        }

        public static void teleporterAI(NPC npc, ref float[] ai, bool immobile = true, int tpRadius = 20, int distFromPlayer = 4, int tpInterval = 650, bool aerial = true, Action<bool> tpEffects = null, bool tpConstant = false)
        {
            npc.TargetClosest(true);
            Vector2 telePos = new Vector2(0, 0);
            if (immobile)
            {
                npc.velocity.X *= 0.93f;
                if ((double)Math.Abs(npc.velocity.X) > 0.1) npc.velocity.X = 0f;
            }
            if (tpConstant) ai[3]++;
            if (ai[3] >= tpInterval)
            {
                int playerTileX = (int)Main.player[npc.target].position.X / 16;
                int playerTileY = (int)Main.player[npc.target].position.Y / 16;
                int tileX = (int)npc.position.X / 16;
                int tileY = (int)npc.position.Y / 16;
                int tpCheck = 0;
                bool hasTP = false;
                if (Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 2000f)
                {
                    tpCheck = 100;
                    hasTP = true;
                }
                while (!hasTP && tpCheck < 100)
                {
                    tpCheck++;
                    int tpTileX = TempSyncedRand.Next(playerTileX - tpRadius, playerTileX + tpRadius);
                    int tpTileY = TempSyncedRand.Next(playerTileY - tpRadius, playerTileY + tpRadius);
                    for (int tpY = tpTileY; tpY < playerTileY + tpRadius; tpY++)
                    {
                        if ((tpY < playerTileY - distFromPlayer || tpY > playerTileY + distFromPlayer || tpTileX < playerTileX - distFromPlayer || tpTileX > playerTileX + distFromPlayer) && (tpY < tileY - 1 || tpY > tileY + 1 || tpTileX < tileX - 1 || tpTileX > tileX + 1) && Main.tile[tpTileX, tpY].HasTile)
                        {
                            bool safe = true;
                            if (Main.tile[tpTileX, tpY - 1].LiquidAmount > 0 && Main.tile[tpTileX, tpY - 1].LiquidType == LiquidID.Lava && !npc.lavaImmune) safe = false;
                            if (safe && (Main.tileSolid[(int)Main.tile[tpTileX, tpY].TileType] || aerial) && !Collision.SolidTiles(tpTileX - 1, tpTileX + 1, tpY - 4, tpY - 1))
                            {
                                telePos.X = (float)(tpTileX * 16f - (float)(npc.width / 2) + 8f);
                                telePos.Y = (aerial) ? (float)(tpY * 16f - (float)npc.height) - 65 : (float)(tpY * 16f - (float)npc.height);
                                hasTP = true;
                                ai[3] = -120f;
                                break;
                            }
                        }
                    }
                }
                npc.netUpdate = true;
            }
            if (ai[3] == -120f && telePos != new Vector2(0, 0))
            {
                if (tpEffects != null) { tpEffects(true); }
                npc.position.X = telePos.X;
                npc.position.Y = telePos.Y;
                npc.velocity *= 0f;
                ai[3] = 0f;
                if (tpEffects != null) { tpEffects(false); }
            }
        }

        public static void fighterAI(NPC npc, ref float[] ai, bool nocturnal = true, bool focused = false, int boredom = 60, int knockPower = 0, float accel = 0.07f, float topSpeed = 1f, float leapReq = 0, float leapSpeed = 3, float leapHeight = 4, float leapRangeX = 100, float leapRangeY = 50, int shotType = 0, float shotRate = 70, int shotPow = -1, float shotSpeed = 11)
        {
            bool moonwalking = false;
            if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
                moonwalking = true;
            if (shotType <= 0 || npc.ai[2] <= 0f)
            {
                if (npc.position.X == npc.oldPosition.X || ai[3] >= (float)boredom || moonwalking)
                { ai[3] += 1f; }
                else if ((double)Math.Abs(npc.velocity.X) > 0.9 && ai[3] > 0f)
                { ai[3] -= 1f; }
                if ((npc.justHit && !focused) || ai[3] > (float)(boredom * 10))
                { ai[3] = 0f; }
                if (ai[3] == (float)boredom)
                { npc.netUpdate = true; }
            }
            if (!focused && (npc.justHit || npc.confused))
            {
                if (shotType > 0)
                { npc.localAI[0] = (float)shotRate / 3; }
                npc.ai[2] = 0;
            }
            bool notBored = ai[3] < (float)boredom;
            if ((!nocturnal || !Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0) && notBored)
            { npc.TargetClosest(true); }
            else if (shotType <= 0 || ai[2] <= 0f)
            {
                if (nocturnal && Main.dayTime && (double)(npc.position.Y / 16f) < Main.worldSurface && npc.timeLeft > 10)
                { npc.timeLeft = 10; }
                if (npc.velocity.X == 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        if (npc.ai[0] == 0f)
                        { npc.ai[0] = 1f; }
                        else
                        {
                            npc.direction *= -1;
                            npc.spriteDirection = npc.direction;
                            npc.ai[0] = 0f;
                        }
                    }
                }
                else
                { ai[0] = 0f; }
                if (npc.direction == 0)
                { npc.direction = 1; }
            }
            if (shotType <= 0 || (npc.ai[2] <= 0f && !npc.confused))
            {
                if (Math.Abs(npc.velocity.X) > topSpeed)
                {
                    if (npc.velocity.Y == 0f)
                    { npc.velocity *= .8f; }
                }
                else if ((npc.velocity.X < topSpeed && npc.direction == 1) || (npc.velocity.X > -topSpeed && npc.direction == -1))
                {
                    npc.velocity.X = npc.velocity.X + (float)npc.direction * accel;
                    if ((float)npc.direction * npc.velocity.X > topSpeed)
                    { npc.velocity.X = (float)npc.direction * topSpeed; }
                }
            }
            if (shotType > 0)
            {
                if (!npc.confused)
                {
                    float distance = npc.Distance(Main.player[npc.target].Center);
                    Vector2 angle = Main.player[npc.target].Center - npc.Center;
                    angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
                    angle.X += (float)Main.rand.Next(-40, 41);
                    angle.Y += (float)Main.rand.Next(-40, 41);
                    angle.Normalize();
                    angle *= shotSpeed;
                    if (npc.localAI[0] > 0f)
                    { npc.localAI[0] -= 1f; }
                    if (npc.ai[2] > 0f)
                    {
                        npc.TargetClosest(true);
                        if (npc.localAI[0] == (float)(shotRate / 2))
                        {
                            int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center.X, npc.Center.Y, angle.X, angle.Y, shotType, shotPow, 0f, Main.myPlayer, -1);
                            Main.projectile[proj].netUpdate = true;
                            if (Math.Abs(angle.Y) > Math.Abs(angle.X) * 2f)
                            {
                                if (angle.Y > 0f)
                                { npc.ai[2] = 1f; }
                                else
                                { npc.ai[2] = 5f; }
                            }
                            else if (Math.Abs(angle.X) > Math.Abs(angle.Y) * 2f)
                            { npc.ai[2] = 3f; }
                            else if (angle.Y > 0f)
                            { npc.ai[2] = 2f; }
                            else
                            { npc.ai[2] = 4f; }
                        }
                        if (npc.velocity.Y != 0f || npc.localAI[0] <= 0f)
                        {
                            npc.ai[2] = 0f;
                            npc.localAI[0] = 0f;
                        }
                        else
                        {
                            npc.velocity.X *= 0.9f;
                            npc.spriteDirection = npc.direction;
                        }
                    }
                    if (npc.ai[2] <= 0f && npc.velocity.Y == 0f && npc.localAI[0] <= 0f && !Main.player[npc.target].dead && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && distance < 700f)
                    {
                        npc.netUpdate = true;
                        npc.velocity.X *= 0.5f;
                        npc.ai[2] = 3f;
                        npc.localAI[0] = (float)shotRate;
                    }
                }
            }
            bool standingOnSolid = false;
            if (npc.velocity.Y == 0f)
            {
                int yBelow = (int)(npc.position.Y + (float)npc.height + 7f) / 16;
                int xLeft = (int)npc.position.X / 16;
                int xRight = (int)(npc.position.X + (float)npc.width) / 16;
                for (int l = xLeft; l <= xRight; l++)
                {
                    if (Main.tile[l, yBelow] == null)
                        return;

                    if (Main.tile[l, yBelow].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[l, yBelow].TileType])
                    {
                        standingOnSolid = true;
                        break;
                    }
                }
            }
            if (npc.velocity.Y >= 0f)
            {
                int offset = 0;
                if (npc.velocity.X < 0f) offset = -1;
                if (npc.velocity.X > 0f) offset = 1;
                Vector2 pos = npc.position;
                pos.X += npc.velocity.X;
                int tileX = (int)((pos.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * offset)) / 16f);
                int tileY = (int)((pos.Y + (float)npc.height - 1f) / 16f);
                if ((float)(tileX * 16) < pos.X + (float)npc.width && (float)(tileX * 16 + 16) > pos.X && ((Main.tile[tileX, tileY].HasUnactuatedTile && !Main.tile[tileX, tileY].TopSlope && !Main.tile[tileX, tileY - 1].TopSlope && Main.tileSolid[(int)Main.tile[tileX, tileY].TileType] && !Main.tileSolidTop[(int)Main.tile[tileX, tileY].TileType]) || (Main.tile[tileX, tileY - 1].IsHalfBlock && Main.tile[tileX, tileY - 1].HasUnactuatedTile)) && (!Main.tile[tileX, tileY - 1].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[tileX, tileY - 1].TileType] || Main.tileSolidTop[(int)Main.tile[tileX, tileY - 1].TileType] || (Main.tile[tileX, tileY - 1].IsHalfBlock && (!Main.tile[tileX, tileY - 4].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[tileX, tileY - 4].TileType] || Main.tileSolidTop[(int)Main.tile[tileX, tileY - 4].TileType]))) && (!Main.tile[tileX, tileY - 2].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[tileX, tileY - 2].TileType] || Main.tileSolidTop[(int)Main.tile[tileX, tileY - 2].TileType]) && (!Main.tile[tileX, tileY - 3].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[tileX, tileY - 3].TileType] || Main.tileSolidTop[(int)Main.tile[tileX, tileY - 3].TileType]) && (!Main.tile[tileX - offset, tileY - 3].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[tileX - offset, tileY - 3].TileType]))
                {
                    float tileWorldY = (float)(tileY * 16);
                    if (Main.tile[tileX, tileY].IsHalfBlock)
                    { tileWorldY += 8f; }
                    if (Main.tile[tileX, tileY - 1].IsHalfBlock)
                    { tileWorldY -= 8f; }
                    if (tileWorldY < pos.Y + (float)npc.height)
                    {
                        float tileWorldYHeight = pos.Y + (float)npc.height - tileWorldY;
                        float heightNeeded = 16.1f;
                        if (tileWorldYHeight <= heightNeeded)
                        {
                            npc.gfxOffY += npc.position.Y + (float)npc.height - tileWorldY;
                            npc.position.Y = tileWorldY - (float)npc.height;
                            npc.stepSpeed = (float)((double)tileWorldYHeight >= 9.0 ? 2f : 1f);
                        }
                    }
                }
            }
            if (standingOnSolid)
            {
                int x2 = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f);
                int y2 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
                if (leapReq > 1)
                { x2 = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f); }
                if (Main.tile[x2, y2 - 1].HasUnactuatedTile && (Main.tile[x2, y2 - 1].TileType == 10 || Main.tile[x2, y2 - 1].TileType == 388) && knockPower != 0)
                {
                    npc.localAI[2] += 1f;
                    ai[3] = 0f;
                    if (npc.localAI[2] >= 60f)
                    {
                        npc.velocity.X = 0.5f * (float)(-(float)npc.direction);
                        npc.localAI[1] += Math.Abs(knockPower);
                        npc.localAI[2] = 0f;
                        bool doorBuster = false;
                        if (npc.localAI[1] >= 10f)
                        {
                            doorBuster = true;
                            npc.localAI[1] = 10f;
                        }
                        WorldGen.KillTile(x2, y2 - 1, true, false, false);
                        if ((Main.netMode != 1 || !doorBuster) && doorBuster && Main.netMode != 1)
                        {
                            if (knockPower < 0)
                            {
                                WorldGen.KillTile(x2, y2 - 1, false, false, false);
                                if (Main.netMode == 2)
                                    NetMessage.SendData(17, -1, -1, null, 0, (float)x2, (float)(y2 - 1), 0f, 0);
                            }
                            else
                            {
                                if (Main.tile[x2, y2 - 1].TileType == 10)
                                {
                                    bool openDoor = WorldGen.OpenDoor(x2, y2 - 1, npc.direction);
                                    if (!openDoor)
                                    {
                                        ai[3] = (float)boredom;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == 2 && openDoor)
                                        NetMessage.SendData(19, -1, -1, null, 0, (float)x2, (float)(y2 - 1), (float)npc.direction, 0, 0, 0);
                                }
                                if (Main.tile[x2, y2 - 1].TileType == 388)
                                {
                                    bool openDoor = WorldGen.ShiftTallGate(x2, y2 - 1, false);
                                    if (!openDoor)
                                    {
                                        ai[3] = (float)boredom;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == 2 && openDoor)
                                        NetMessage.SendData(19, -1, -1, null, 4, (float)x2, (float)(y2 - 1), 0f, 0, 0, 0);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                    {
                        if (npc.height >= 32 && Main.tile[x2, y2 - 2].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[x2, y2 - 2].TileType])
                        {
                            if (Main.tile[x2, y2 - 3].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[x2, y2 - 3].TileType])
                            {
                                npc.velocity.Y = -8f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.velocity.Y = -7f;
                                npc.netUpdate = true;
                            }
                        }
                        else if (Main.tile[x2, y2 - 1].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[x2, y2 - 1].TileType])
                        {
                            npc.velocity.Y = -6f;
                            npc.netUpdate = true;
                        }
                        else if (npc.position.Y + (float)npc.height - (float)(y2 * 16) > 20f && Main.tile[x2, y2].HasUnactuatedTile && !Main.tile[x2, y2].TopSlope && Main.tileSolid[(int)Main.tile[x2, y2].TileType])
                        {
                            npc.velocity.Y = -5f;
                            npc.netUpdate = true;
                        }
                        else if (leapReq > -1 && npc.directionY < 0 && (!Main.tile[x2, y2 + 1].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[x2, y2 + 1].TileType]) && (!Main.tile[x2 + npc.direction, y2 + 1].HasUnactuatedTile || !Main.tileSolid[(int)Main.tile[x2 + npc.direction, y2 + 1].TileType]))
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X *= 1.5f;
                            npc.netUpdate = true;
                        }
                        else if (knockPower != 0)
                        {
                            npc.localAI[1] = 0f;
                            npc.localAI[2] = 0f;
                        }
                        if (npc.velocity.Y == 0f && !npc.justHit && ai[3] == 1f)
                        { npc.velocity.Y = -5f; }
                    }
                    if (leapReq > 1 && npc.velocity.Y == 0f && Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < leapRangeX && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < leapRangeY && (float)npc.direction * npc.velocity.X >= leapReq)
                    {
                        npc.velocity.X *= 2f;
                        if ((float)npc.direction * npc.velocity.X > leapSpeed)
                        { npc.velocity.X = (float)npc.direction * leapSpeed; }
                        npc.velocity.Y = -leapHeight;
                        npc.netUpdate = true;
                    }
                }
            }
            else if (knockPower != 0)
            {
                npc.localAI[1] = 0f;
                npc.localAI[2] = 0f;
            }
        }
    }
}
