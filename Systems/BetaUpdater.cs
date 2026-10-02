using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{
    public enum BetaUpdateStatus
    {
        None,
        Downloading,
        Installing,
        Failed
    }

    /// <summary>
    /// Self-updater for the unreleased beta build that lives outside the Steam Workshop. Only players already on a
    /// hand-installed beta are opted in (see CheckForUpdateAsync). The remote .tmod's version is read from its header
    /// with a ranged request (no separate version file). A newer build is downloaded automatically and swapped in with a
    /// mod reload the next time the main menu is showing. tModLoader loads the highest Version among every copy of a
    /// mod it finds, so the download goes into Mods\ under a different, versioned filename (the running .tmod is
    /// file-locked) and the reload picks it up without restarting the game.
    /// </summary>
    public static class BetaUpdater
    {
        public static BetaUpdateStatus Status = BetaUpdateStatus.None;
        public static Version RemoteVersion;
        public static int DownloadPercent;

        private const string ModName = "tsorcRevamp";
        private const string BetaFilePrefix = "tsorcRevamp_Beta_";
        private const int HeaderBytesToRead = 1024; // magic + tML version + 20 hash + 256 signature + int + name + version is ~314 bytes
        private const double RecheckMinutes = 30;

        // Set by the download thread, consumed on the main thread in DrawMenuButton: ModLoader.Reload must not be called
        // off-thread, and must not run while the player is in a world (DrawMenu only runs at the main menu).
        private static bool reloadRequested;
        private static bool downloading;
        private static bool checking;
        private static DateTime lastCheckUtc = DateTime.MinValue;

        private sealed class TmodHeader
        {
            public string Name;
            public Version Version;
            public Version TmlVersion;
            public byte[] Hash;
            public long HashStartPosition; // file offset just past the length int; the SHA1 covers everything from here to EOF
            public int DataLength;         // byte count the SHA1 covers, as claimed by the header
        }

        private static string DataDirectory => Path.Combine(Main.SavePath, "ModConfigs", "tsorcRevampData");

        // Reads the fixed .tmod header layout (mirrors TmodFile.Read). Returns null if the stream isn't a readable tmod.
        private static TmodHeader ReadTmodHeader(Stream stream)
        {
            try
            {
                using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));

                if (magic != "TMOD")
                {
                    return null;
                }

                TmodHeader header = new TmodHeader();
                header.TmlVersion = new Version(reader.ReadString());
                header.Hash = reader.ReadBytes(20);
                reader.ReadBytes(256); // signature
                header.DataLength = reader.ReadInt32();
                header.HashStartPosition = stream.Position;
                header.Name = reader.ReadString();
                header.Version = new Version(reader.ReadString());

                return header;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// If this player is on the beta, finds the newest version on GitHub and downloads it. Safe to fire and forget;
        /// re-run from the main menu every RecheckMinutes.
        /// </summary>
        public static async Task CheckForUpdateAsync()
        {
            if (checking || downloading)
            {
                return;
            }

            checking = true;
            lastCheckUtc = DateTime.UtcNow;

            log4net.ILog logger = ModContent.GetInstance<tsorcRevamp>().Logger;
            Version runningVersion = ModContent.GetInstance<tsorcRevamp>().Version;
            string optInPath = Path.Combine(DataDirectory, "tsorcBetaOptIn.txt");

            try
            {
                // One pass over Mods\: (a) a tsorcRevamp .tmod at the running version means we're running a local copy
                // rather than the Workshop one, i.e. a hand-installed beta (this also catches a build tModLoader
                // downloaded from a server on join) and (b) beta files from earlier updates that are now outdated.
                // The file we're actually running from is locked, so its delete fails and is skipped.
                bool runningLocalCopy = false;

                foreach (string modFile in Directory.GetFiles(ModLoader.ModPath, "*.tmod"))
                {
                    TmodHeader fileHeader = null;

                    using (FileStream fileStream = File.OpenRead(modFile))
                    {
                        fileHeader = ReadTmodHeader(fileStream);
                    }

                    if (fileHeader == null || fileHeader.Name != ModName)
                    {
                        continue;
                    }

                    if (fileHeader.Version == runningVersion)
                    {
                        runningLocalCopy = true;
                    }

                    bool isBetaFile = Path.GetFileName(modFile).StartsWith(BetaFilePrefix);

                    if (isBetaFile && fileHeader.Version < runningVersion)
                    {
                        try
                        {
                            File.Delete(modFile);
                        }
                        catch (IOException)
                        {
                            // locked: still loaded by this session
                        }
                    }
                }

                // Opt-in is permanent once seen, so the Workshop later releasing a higher version doesn't silently
                // end the player's beta subscription. Delete tsorcBetaOptIn.txt to opt back out.
                if (!File.Exists(optInPath) && runningLocalCopy)
                {
                    Directory.CreateDirectory(DataDirectory);
                    File.WriteAllText(optInPath, "Written when a hand-installed (non-Workshop) tsorcRevamp " + runningVersion + " was first seen running. Delete this file to stop beta auto-updates.");
                    logger.Info("Beta updater: non-Workshop build detected, opted in to beta auto-updates.");
                }

                if (!File.Exists(optInPath))
                {
                    return;
                }

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, VariousConstants.BETA_UPDATE_URL);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, HeaderBytesToRead - 1);

                // ResponseHeadersRead + disposing early means that even if the host ignores Range and starts a full
                // 60MB+ response, we only ever pull the first KB off the wire.
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using Stream responseStream = await response.Content.ReadAsStreamAsync();
                byte[] headerBytes = new byte[HeaderBytesToRead];
                int bytesRead = await responseStream.ReadAtLeastAsync(headerBytes, HeaderBytesToRead, throwOnEndOfStream: false);

                TmodHeader remoteHeader = ReadTmodHeader(new MemoryStream(headerBytes, 0, bytesRead));

                if (remoteHeader == null || remoteHeader.Name != ModName)
                {
                    logger.Warn("Beta updater: remote file isn't a readable tsorcRevamp .tmod (LFS pointer or moved file?).");
                    return;
                }

                if (remoteHeader.Version <= runningVersion)
                {
                    return;
                }

                // tModLoader ignores a mod built for a different monthly release, so loading it would just reload back
                // to the old version and we'd loop forever.
                if (remoteHeader.TmlVersion.Major != BuildInfo.tMLVersion.Major || remoteHeader.TmlVersion.Minor != BuildInfo.tMLVersion.Minor)
                {
                    logger.Warn("Beta updater: remote " + remoteHeader.Version + " is built for tModLoader " + remoteHeader.TmlVersion + " but this is " + BuildInfo.tMLVersion + "; skipping.");
                    return;
                }

                // Loop guard: DownloadAndInstallAsync records the version it reloaded for. If we're STILL older than that
                // version after the reload, tModLoader refused to load it for some reason; don't retry it every launch.
                string lastAttemptPath = Path.Combine(DataDirectory, "tsorcBetaLastAttempt.txt");

                if (File.Exists(lastAttemptPath) && File.ReadAllText(lastAttemptPath).Trim() == remoteHeader.Version.ToString())
                {
                    logger.Warn("Beta updater: already tried to install " + remoteHeader.Version + " and it didn't take effect; not retrying until a newer version is uploaded.");
                    return;
                }

                RemoteVersion = remoteHeader.Version;
                logger.Info("Beta updater: " + runningVersion + " -> " + RemoteVersion + ", downloading.");
            }
            catch (Exception exception)
            {
                logger.Warn("Beta updater check failed: " + exception.Message);
                return;
            }
            finally
            {
                checking = false;
            }

            await DownloadAndInstallAsync();
        }

        /// <summary>Downloads the beta .tmod, verifies its SHA1, drops it into Mods\ and queues a mod reload.</summary>
        public static async Task DownloadAndInstallAsync()
        {
            if (downloading)
            {
                return;
            }

            downloading = true;
            Status = BetaUpdateStatus.Downloading;
            DownloadPercent = 0;

            log4net.ILog logger = ModContent.GetInstance<tsorcRevamp>().Logger;
            string tempPath = Path.Combine(DataDirectory, "tsorcRevampBeta.tmod.download");

            try
            {
                Directory.CreateDirectory(DataDirectory);

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);

                using HttpResponseMessage response = await client.GetAsync(VariousConstants.BETA_UPDATE_URL, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                long receivedBytes = 0;

                using (Stream source = await response.Content.ReadAsStreamAsync())
                using (FileStream destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[81920];
                    int chunkSize;

                    while ((chunkSize = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destination.WriteAsync(buffer, 0, chunkSize);
                        receivedBytes += chunkSize;

                        if (totalBytes > 0)
                        {
                            DownloadPercent = (int)(receivedBytes * 100 / totalBytes);
                        }
                    }
                }

                // Same integrity check tModLoader itself runs at load: the header's SHA1 must match everything after it.
                TmodHeader downloadedHeader = null;
                bool hashMatches = false;

                using (FileStream verifyStream = File.OpenRead(tempPath))
                {
                    downloadedHeader = ReadTmodHeader(verifyStream);

                    if (downloadedHeader != null && verifyStream.Length - downloadedHeader.HashStartPosition == downloadedHeader.DataLength)
                    {
                        verifyStream.Position = downloadedHeader.HashStartPosition;
                        byte[] computedHash = SHA1.HashData(verifyStream);
                        hashMatches = computedHash.AsSpan().SequenceEqual(downloadedHeader.Hash);
                    }
                }

                if (downloadedHeader == null || downloadedHeader.Name != ModName || !hashMatches)
                {
                    throw new InvalidDataException("Downloaded beta .tmod is corrupt or incomplete.");
                }

                string installedPath = Path.Combine(ModLoader.ModPath, BetaFilePrefix + downloadedHeader.Version + ".tmod");
                File.Move(tempPath, installedPath, overwrite: true);

                // Written BEFORE the reload so the loop guard in CheckForUpdateAsync survives it.
                File.WriteAllText(Path.Combine(DataDirectory, "tsorcBetaLastAttempt.txt"), downloadedHeader.Version.ToString());

                logger.Info("Beta update " + downloadedHeader.Version + " installed to " + installedPath + ", reloading mods when the main menu is showing.");
                Status = BetaUpdateStatus.Installing;
                reloadRequested = true;
            }
            catch (Exception exception)
            {
                logger.Warn("Beta update download failed: " + exception.Message);
                Status = BetaUpdateStatus.Failed;

                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // best effort
                }
            }
            finally
            {
                downloading = false;
            }
        }

        /// <summary>Called every frame the main menu is open (from the DrawMenu hook): runs the queued reload, re-checks periodically, draws the status line.</summary>
        public static void DrawMenuButton()
        {
            if (reloadRequested)
            {
                reloadRequested = false;
                typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { });
                return;
            }

            bool recheckDue = (DateTime.UtcNow - lastCheckUtc).TotalMinutes > RecheckMinutes;

            if (Status == BetaUpdateStatus.None && recheckDue && !checking && !downloading)
            {
                _ = CheckForUpdateAsync();
            }

            if (Status == BetaUpdateStatus.None)
            {
                return;
            }

            string text = "";

            if (Status == BetaUpdateStatus.Downloading)
            {
                text = LangUtils.GetTextValue("UI.BetaUpdateDownloading", RemoteVersion, DownloadPercent);
            }
            else if (Status == BetaUpdateStatus.Installing)
            {
                text = LangUtils.GetTextValue("UI.BetaUpdateInstalling");
            }
            else if (Status == BetaUpdateStatus.Failed)
            {
                text = LangUtils.GetTextValue("UI.BetaUpdateFailed");
            }

            const float textScale = 1.5f;
            const float textTop = 570f; // above the music (610) / map (650, 690) lines in MethodSwaps.DownloadMapButton

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * textScale;
            Vector2 textPosition = new Vector2(Main.screenWidth / 2f - textSize.X / 2f, textTop);
            Rectangle textBounds = new Rectangle((int)textPosition.X, (int)textPosition.Y, (int)textSize.X, (int)textSize.Y);

            bool clickable = Status == BetaUpdateStatus.Failed;
            bool hovering = clickable && textBounds.Contains(Main.mouseX, Main.mouseY);
            Color textColor = Main.DiscoColor;

            if (hovering)
            {
                textColor = Color.Yellow;

                // mouseLeftRelease is true only on the first frame after the button was let go, so this fires once per click.
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    _ = DownloadAndInstallAsync();
                }
            }

            Main.spriteBatch.Begin();
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, text, textPosition + new Vector2(2f, 2f), Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, text, textPosition, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }
    }
}
