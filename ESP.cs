using AotForms;
using Client;
using Guna.UI2.AnimatorNS;
using Guna.UI2.WinForms;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Management;
using System.Net.Http.Json;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using x;
using static AotForms.WinAPI;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static TheArtOfDevHtmlRenderer.Adapters.RGraphicsPath;

namespace AotForms
{
    public static class FontManager
    {
        public static ImFontPtr VerdanaSmall;
        public static ImFontPtr VerdanaNormal;
        public static ImFontPtr VerdanaBig;

        public static ImFontPtr InterSmall;
        public static ImFontPtr InterNormal;
        public static ImFontPtr InterBig;
    }


    internal class ESP : ClickableTransparentOverlay.Overlay
    {
        public static int EnemyCount = 0;

        IntPtr hWnd;
        IntPtr HDPlayer;
        IntPtr BluestackAppPlayer;


        bool show = true;
        private bool isAutoRefreshActive = false;
        private bool refreshTaskStarted = false;

        private void AutoRefreshLoop()
        {
            while (true) 
            {
                Thread.Sleep(3000);
                if (isAutoRefreshActive)
                {
                    InternalMemory.Cache = new();
                    Core.Entities = new();
                }
            }
        }
        private Dictionary<int, (Vector2 head, Vector2 bottom)> lastPositions = new();

        private static Dictionary<string, (IntPtr handle, uint width, uint height)> weaponIconCache
    = new Dictionary<string, (IntPtr, uint, uint)>();

        private (IntPtr handle, uint width, uint height) GetWeaponIcon(string weaponName)
        {
            string key = weaponName.ToLower();
            if (weaponIconCache.TryGetValue(key, out var cache))
                return cache;

            string imagePath = $"C:\\Extracted\\ESPWeapon\\ESPWeapon\\{key}.png";
            if (!File.Exists(imagePath))
                return (IntPtr.Zero, 0, 0);

            AddOrGetImagePointer(imagePath, true, out IntPtr handle, out uint width, out uint height);
            if (handle != IntPtr.Zero)
            {
                weaponIconCache[key] = (handle, width, height);
                return (handle, width, height);
            }
            return (IntPtr.Zero, 0, 0);
        }

        private (Vector2 head, Vector2 bottom) GetSmoothedPosition(int id, Vector2 head, Vector2 bottom)
        {
            if (head.X <= 0 || head.Y <= 0 || bottom.X <= 0 || bottom.Y <= 0)
            {
                if (lastPositions.TryGetValue(id, out var last))
                    return last;
                else
                    return (head, bottom);
            }
            else
            {
                lastPositions[id] = (head, bottom);
                return (head, bottom);
            }
        }

        protected override unsafe void Render()
        {

            if (!refreshTaskStarted)
            {
                new Thread(AutoRefreshLoop) { IsBackground = true }.Start();
                refreshTaskStarted = true;
            }
            HandleKeybinds();
            RenderImgui();
            CreateHandle();

            if (Core.Entities == null || Core.Entities.Count == 0)
            {
                EnemyCount = 0;
                return;
            }
            var entitiesToRender = Core.Entities.Values.ToList();

            EnemyCount = entitiesToRender.Count(entity => entity != null && !entity.IsDead && entity.IsKnown);

            string windowName = "Gazan VN";
            hWnd = FindWindow(null, windowName);
            HDPlayer = FindWindow("BlueStacksApp", null);

            foreach (var entity in entitiesToRender)
            {
                if (entity == null || !entity.IsKnown || entity.IsDead) continue;
                var dist = Vector3.Distance(Core.LocalMainCamera, entity.Head);

                if (dist > 600) continue;

                var rawHead = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                var rawBottom = W2S.WorldToScreen(Core.CameraMatrix, entity.Root, Core.Width, Core.Height);

                var (headScreenPos, bottomScreenPos) = GetSmoothedPosition(entity.GetHashCode(), rawHead, rawBottom);

                if (headScreenPos.X < 1 || headScreenPos.Y < 1) continue;
                float CornerHeight = Math.Abs(headScreenPos.Y - bottomScreenPos.Y);

                float yOffset = CornerHeight * 0.1f;
                Vector2 targetPos = new Vector2(headScreenPos.X, headScreenPos.Y - yOffset);

                if (headScreenPos.X < 1 || headScreenPos.Y < 1) continue;
                if (bottomScreenPos.X < 1 || bottomScreenPos.Y < 1) continue;

                float CornerWidth = (float)(CornerHeight * 0.65);

                if (Config.ESPEnabled)
                {
                    if (Config.ESPLineTren)
                    {
                        float height = Math.Abs(bottomScreenPos.Y - headScreenPos.Y);
                        float headFix = height * 0.15f;

                        Vector2 realHead = new Vector2(headScreenPos.X, headScreenPos.Y - headFix);

                        ImGui.GetBackgroundDrawList().AddLine(
                            new Vector2(Core.Width / 2f, 0f),
                            realHead,
                            ColorToUint32(Config.ESPLineColor),
                            0.7f
                        );
                    }

                    if (Config.ESPWeaponIcon && !string.IsNullOrEmpty(entity.WeaponName))
                    {
                        if (headScreenPos.X >= 0 && headScreenPos.Y >= 0 &&
                            headScreenPos.X <= Core.Width && headScreenPos.Y <= Core.Height)
                        {
                            Vector2 fixedNameSize1 = new Vector2(95, 16);
                            Vector2 namePos = new Vector2(
                                headScreenPos.X - fixedNameSize1.X / 2,
                                headScreenPos.Y - fixedNameSize1.Y - 13);

                            var (imageHandle, width, height) = GetWeaponIcon(entity.WeaponName);
                            if (imageHandle != IntPtr.Zero)
                            {
                                Vector2 iconSize = new Vector2(60, 20);

                                Vector2 iconPos = new Vector2(
                                    namePos.X + (fixedNameSize1.X - iconSize.X) / 2,
                                    namePos.Y - iconSize.Y - 8);

                                ImGui.GetForegroundDrawList().AddImage(imageHandle, iconPos, iconPos + iconSize);
                            }
                        }
                    }
                    if (Config.ESPFillBox)
                    {
                        float height = Math.Abs(bottomScreenPos.Y - headScreenPos.Y);

                        float headFix = height * 0.15f;
                        float newHeadY = headScreenPos.Y - headFix;

                        float width = height * 0.5f;
                        Vector2 topLeft = new Vector2(headScreenPos.X - width / 2f, newHeadY);
                        Vector2 bottomRight = new Vector2(headScreenPos.X + width / 2f, bottomScreenPos.Y);

                        float alpha = 0.5f;
                        Color fillColor = Color.FromArgb(120, 20, 20, 20);
                        ImGui.GetForegroundDrawList().AddRectFilled(topLeft, bottomRight, ColorToUint32(fillColor));

                        ImGui.GetForegroundDrawList().AddRectFilled(
                            topLeft,
                            bottomRight,
                            ColorToUint32(fillColor)
                        );
                    }

                    if (Config.ESPBox || Config.ESPBox2)
                    {
                        float height = Math.Abs(bottomScreenPos.Y - headScreenPos.Y);

                        float headFix = height * 0.15f;
                        float newHeadY = headScreenPos.Y - headFix;


                        float width = height * 0.5f;


                        Vector2 topLeft = new Vector2(headScreenPos.X - width / 2f, newHeadY);
                        Vector2 bottomRight = new Vector2(headScreenPos.X + width / 2f, bottomScreenPos.Y);


                        if (Config.ESPBox)
                        {
                            uint boxColor = ColorToUint32(Config.ESPBoxColor);
                            ImGui.GetForegroundDrawList().AddRect(
                            topLeft,
                            bottomRight,
                            boxColor,
                            0f,
                            ImDrawFlags.None,
                            1.2f
                            );
                        }


                        if (Config.ESPBox2)
                        {
                            uint boxColor = ColorToUint32(Config.ESPBoxCColor);
                            DrawCorneredBox(topLeft.X, topLeft.Y, width, height + headFix, boxColor, 1.2f);
                        }
                    }
                    if (Config.ESPName)
                    {
                        ImGui.PushFont(FontManager.InterSmall);
                        ESPNameRender(entity, headScreenPos, dist);
                        ImGui.PopFont();
                    }

                    if (Config.ESPHealth)
                    {
                        //DrawHealthBar(entity.Health, 200, headScreenPos.X - (CornerWidth / 2) - 5, headScreenPos.Y, CornerHeight);
                        DrawHealthBarVertical(entity, headScreenPos, bottomScreenPos, offsetX: 4f, offsetY: 0f, anchorRight: true);
                    }
                    if (Config.ESPSkeleton)
                    {
                        DrawSkeleton(entity);
                    }
                }
                if (Config.FOVMouseEnabled)
                {
                    var drawList = ImGui.GetForegroundDrawList();
                    var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

                    float radius = Config.FOVMouseRadius;
                    int segments = Math.Clamp((int)(radius / 2f), 64, 180);

                    drawList.AddCircle(
            screenCenter,
            radius,
            ImGui.GetColorU32(Config.FOVColor),
            segments,
            0.15f // thickness mảnh
        );

                    if (AimbotV2.CurrentTarget != null)
                    {
                        var head2D = W2S.WorldToScreen(Core.CameraMatrix, AimbotV2.CurrentTarget.Head, Core.Width, Core.Height);

                        if (head2D != Vector2.Zero)
                        {
                            float distance = Vector2.Distance(screenCenter, head2D);
                            if (distance < Config.FOVMouseRadius)
                            {
                                drawList.AddLine(
                                    screenCenter,
                                    head2D,
                                    ImGui.GetColorU32(new Vector4(1, 0, 0, 1)),
                                    1.5f
                                );
                            }
                        }
                    }
                }
                if (Config.ShowText)
                {
                    ImGui.PushFont(FontManager.VerdanaSmall);

                    var vList = ImGui.GetForegroundDrawList();

                    string aimbotText = $"Aimbot : {(Config.AimbotV2Enabled ? "ON" : "OFF")}";
                    string teleportText = $"Teleport : {(Config.TeleportV2 ? "ON" : "OFF")}";
                    string pullText = $"Pull Enemies : {(Config.PullEnemies ? "ON" : "OFF")}";

                    string fullText = $"{aimbotText} - {teleportText} - {pullText}";

                    var size = ImGui.CalcTextSize(fullText);
                    var pos = new System.Numerics.Vector2(Core.Width / 2f - size.X / 2f, 10);

                    Vector4 green = new(0, 1, 0, 1);
                    Vector4 red = new(1, 0, 0, 1);
                    Vector4 white = new(1, 1, 1, 1);

                    uint colAimbot = ImGui.ColorConvertFloat4ToU32(Config.AimbotV2Enabled ? green : red);
                    uint colTeleport = ImGui.ColorConvertFloat4ToU32(Config.TeleportV2 ? green : red);
                    uint colPull = ImGui.ColorConvertFloat4ToU32(Config.PullEnemies ? green : red);
                    uint colWhite = ImGui.ColorConvertFloat4ToU32(white);

                    float x = pos.X;
                    float y = pos.Y;

                    ImFontPtr font = FontManager.VerdanaSmall;

                    vList.AddText(font, font.FontSize, new(x, y), colAimbot, aimbotText);
                    x += ImGui.CalcTextSize(aimbotText).X;

                    string sep = " - ";
                    vList.AddText(font, font.FontSize, new(x, y), colWhite, sep);
                    x += ImGui.CalcTextSize(sep).X;

                    vList.AddText(font, font.FontSize, new(x, y), colTeleport, teleportText);
                    x += ImGui.CalcTextSize(teleportText).X;

                    vList.AddText(font, font.FontSize, new(x, y), colWhite, sep);
                    x += ImGui.CalcTextSize(sep).X;

                    vList.AddText(font, font.FontSize, new(x, y), colPull, pullText);

                    ImGui.PopFont();
                }
                if (Config.DrawTargetLine && AimbotV2.CurrentTarget != null && AimbotV2.CurrentTarget != null)
                {
                    var drawList = ImGui.GetForegroundDrawList();
                    var center = new Vector2(Core.Width / 2f, Core.Height / 2f);

                    var head2D = W2S.WorldToScreen(Core.CameraMatrix, AimbotV2.CurrentTarget.Head, Core.Width, Core.Height);

                    if (head2D.X > 0 && head2D.Y > 0 && head2D.X < Core.Width && head2D.Y < Core.Height)
                    {
                        drawList.AddLine(center, head2D, ImGui.GetColorU32(new Vector4(1, 0, 0, 1)), 1.5f);
                    }
                }
            }
        }

        private static void ESPNameRender(Entity entity, Vector2 headScreenPos, float dist)
        {
            if (headScreenPos.X >= 0 && headScreenPos.Y >= 0 && headScreenPos.X <= Core.Width && headScreenPos.Y <= Core.Height)
            {
                var vList = ImGui.GetForegroundDrawList();
                var nameText = string.IsNullOrWhiteSpace(entity.Name) ? "GazanVn" +
                    "" : entity.Name;

                float totalWidth = 100f;
                float healthBoxWidth = 26f;
                float nameBoxWidth = totalWidth - healthBoxWidth;
                Vector2 infoBoxSize = new Vector2(totalWidth, 14.5f);
                float rounding = 1.5f;

                Vector2 topLeftPos = new Vector2(headScreenPos.X - totalWidth / 2f, headScreenPos.Y - 30);

                Vector2 healthBoxPos = topLeftPos;
                Vector2 nameBoxPos = new Vector2(healthBoxPos.X + healthBoxWidth, healthBoxPos.Y);

                vList.AddRectFilled(
                    healthBoxPos,
                    healthBoxPos + new Vector2(healthBoxWidth, infoBoxSize.Y),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                    rounding,
                    ImDrawFlags.RoundCornersTopLeft
                );

                vList.AddRectFilled(
                    nameBoxPos,
                    nameBoxPos + new Vector2(nameBoxWidth, infoBoxSize.Y),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.75f)),
                    rounding,
                    ImDrawFlags.RoundCornersTopRight
                );

                float maxHealth;
                if (entity.Health > 230)
                {
                    maxHealth = 500f;
                }
                else if (entity.Health > 200)
                {
                    maxHealth = 230f;
                }
                else
                {
                    maxHealth = 200f;
                }

                int displayHealth = (int)entity.Health;

                string healthStr = displayHealth.ToString();
                Vector2 healthTextSize = ImGui.CalcTextSize(healthStr);
                Vector2 healthTextPos = new Vector2(
                    healthBoxPos.X + (healthBoxWidth - healthTextSize.X) / 2,
                    healthBoxPos.Y + (infoBoxSize.Y - healthTextSize.Y) / 2
                );
                vList.AddText(healthTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), healthStr);

                Vector2 nameTextSize = ImGui.CalcTextSize(nameText);
                Vector2 nameTextPos = new Vector2(
                    nameBoxPos.X + 3,
                    nameBoxPos.Y + (infoBoxSize.Y - nameTextSize.Y) / 2
                );
                vList.AddText(nameTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), nameText);

                float clampedDist = Math.Clamp(dist, 0f, 9999f);
                string distanceText = $"{MathF.Round(clampedDist)}m";
                Vector2 distanceTextSize = ImGui.CalcTextSize(distanceText);
                Vector2 distanceTextPos = new Vector2(
                    nameBoxPos.X + nameBoxWidth - distanceTextSize.X - 4,
                    nameBoxPos.Y + (infoBoxSize.Y - distanceTextSize.Y) / 2
                );
                vList.AddText(distanceTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), distanceText);

                float healthPercent = Math.Clamp((float)displayHealth / maxHealth, 0f, 1f);
                Vector2 barPos = new Vector2(topLeftPos.X, topLeftPos.Y + infoBoxSize.Y);
                Vector2 barSize = new Vector2(totalWidth, 2);

                Vector4 healthBarColor;
                var colorGreen = new Vector4(0, 1, 0, 1);     // 100% Health
                var colorYellow = new Vector4(1, 1, 0, 1);    // 70% Health
                var colorRed = new Vector4(1, 0, 0, 1);       // 40% Health

                if (healthPercent > 0.7f)
                {
                    float t = (healthPercent - 0.7f) / 0.3f;
                    healthBarColor = Vector4.Lerp(colorYellow, colorGreen, t);
                }
                else if (healthPercent > 0.4f)
                {
                    float t = (healthPercent - 0.4f) / 0.3f;
                    healthBarColor = Vector4.Lerp(colorRed, colorYellow, t);
                }
                else
                {
                    healthBarColor = colorRed;
                }
                vList.AddRectFilled(barPos, barPos + barSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.8f)), 2f);
                if (healthPercent > 0)
                {
                    vList.AddRectFilled(
                        barPos,
                        new Vector2(barPos.X + barSize.X * healthPercent, barPos.Y + barSize.Y),
                        ImGui.ColorConvertFloat4ToU32(healthBarColor),
                        2f
                    );
                }
                float triangleWidth = 6f;
                float triangleHeight = 4f;

                Vector2 triangleCenter = new Vector2(topLeftPos.X + totalWidth / 2f, topLeftPos.Y + infoBoxSize.Y + barSize.Y);

                Vector2 p1 = new Vector2(triangleCenter.X, triangleCenter.Y + triangleHeight);
                Vector2 p2 = new Vector2(triangleCenter.X - triangleWidth / 2f, triangleCenter.Y);
                Vector2 p3 = new Vector2(triangleCenter.X + triangleWidth / 2f, triangleCenter.Y);

                vList.AddTriangleFilled(p1, p2, p3, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
            }
        }

        public void DrawCorneredBox(float X, float Y, float W, float H, uint color, float thickness)
        {
            var vList = ImGui.GetForegroundDrawList();

            float lineW = W / 4.5f;
            float lineH = H / 4.5f;

            vList.AddLine(new Vector2(X, Y - thickness / 2), new Vector2(X, Y + lineH), color, thickness);
            vList.AddLine(new Vector2(X - thickness / 2, Y), new Vector2(X + lineW, Y), color, thickness);
            vList.AddLine(new Vector2(X + W - lineW, Y), new Vector2(X + W + thickness / 2, Y), color, thickness);
            vList.AddLine(new Vector2(X + W, Y - thickness / 2), new Vector2(X + W, Y + lineH), color, thickness);
            vList.AddLine(new Vector2(X, Y + H - lineH), new Vector2(X, Y + H + thickness / 2), color, thickness);
            vList.AddLine(new Vector2(X - thickness / 2, Y + H), new Vector2(X + lineW, Y + H), color, thickness);
            vList.AddLine(new Vector2(X + W - lineW, Y + H), new Vector2(X + W + thickness / 2, Y + H), color, thickness);
            vList.AddLine(new Vector2(X + W, Y + H - lineH), new Vector2(X + W, Y + H + thickness / 2), color, thickness);
        }

        private void DrawSkeleton(Entity entity)
        {
            var drawList = ImGui.GetForegroundDrawList();
            uint lineColor = ColorToUint32(Config.ESPSkeletonColor);

            var headScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
            var spineScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Spine, Core.Width, Core.Height);
            var hipScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Hip, Core.Width, Core.Height);

            var rightShoulderScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightSholder, Core.Width, Core.Height);
            var rightElbowScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightElbow, Core.Width, Core.Height);
            var rightWristScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightWrist, Core.Width, Core.Height);
            var rightWristJointScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightWristJoint, Core.Width, Core.Height);

            var leftShoulderScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftSholder, Core.Width, Core.Height);
            var leftElbowScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftElbow, Core.Width, Core.Height);
            var leftHandScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftHand, Core.Width, Core.Height);
            var leftWristJointScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftWristJoint, Core.Width, Core.Height);

            var leftWristScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightWrist, Core.Width, Core.Height);

            var rightCalfScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightCalf, Core.Width, Core.Height);
            var rightFootScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightFoot, Core.Width, Core.Height);

            var leftCalfScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftCalf, Core.Width, Core.Height);
            var leftFootScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.LeftFoot, Core.Width, Core.Height);

            var rootScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Root, Core.Width, Core.Height);

            DrawLine(drawList, spineScreenPos, rightShoulderScreenPos, lineColor); 
            DrawLine(drawList, spineScreenPos, hipScreenPos, lineColor); 
            DrawLine(drawList, spineScreenPos, leftShoulderScreenPos, lineColor); 
            DrawLine(drawList, leftShoulderScreenPos, rightElbowScreenPos, lineColor); 
            DrawLine(drawList, leftElbowScreenPos, rightWristJointScreenPos, lineColor); 
            DrawLine(drawList, rightShoulderScreenPos, leftElbowScreenPos, lineColor); 
            DrawLine(drawList, rightElbowScreenPos, leftWristJointScreenPos, lineColor); 
            DrawLine(drawList, hipScreenPos, rightFootScreenPos, lineColor); 

            DrawLine(drawList, hipScreenPos, leftCalfScreenPos, lineColor);

            uint circleColor = ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            float distance = entity.Distance > 0 ? entity.Distance : 1.0f;
            float baseSize = 60.0f;
            float dynamicRadius = baseSize / distance;
            float circleRadius = Math.Clamp(dynamicRadius, 0.5f, 1.35f);

            if (spineScreenPos.X > 0 && spineScreenPos.Y > 0) 
            {
                drawList.AddCircleFilled(spineScreenPos, circleRadius, circleColor, 30);
            }
            if (rightShoulderScreenPos.X > 0 && rightShoulderScreenPos.Y > 0) 
            {
                drawList.AddCircleFilled(rightShoulderScreenPos, circleRadius, circleColor, 30);
            }
            if (leftShoulderScreenPos.X > 0 && leftShoulderScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(leftShoulderScreenPos, circleRadius, circleColor, 30);
            }
            if (rightElbowScreenPos.X > 0 && rightElbowScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(rightElbowScreenPos, circleRadius, circleColor, 30);
            }
            if (leftElbowScreenPos.X > 0 && leftElbowScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(leftElbowScreenPos, circleRadius, circleColor, 30);
            }
            if (leftWristJointScreenPos.X > 0 && leftWristJointScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(leftWristJointScreenPos, circleRadius, circleColor, 30);
            }
            if (rightWristJointScreenPos.X > 0 && rightWristJointScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(rightWristJointScreenPos, circleRadius, circleColor, 30);
            }
            if (hipScreenPos.X > 0 && hipScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(hipScreenPos, circleRadius, circleColor, 30);
            }
            if (leftCalfScreenPos.X > 0 && leftCalfScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(leftCalfScreenPos, circleRadius, circleColor, 30);
            }
            if (rightFootScreenPos.X > 0 && rightFootScreenPos.Y > 0)
            {
                drawList.AddCircleFilled(rightFootScreenPos, circleRadius, circleColor, 30);
            }
        }
        public enum KeybindState
        {
            None,
            Aimbot,
            Aimbot1,
            Pull,
            Teleport
        }
        private void DrawLine(ImDrawListPtr drawList, Vector2 startPos, Vector2 endPos, uint color)
        {
            if (startPos.X > 0 && startPos.Y > 0 && endPos.X > 0 && endPos.Y > 0)
            {
                drawList.AddLine(startPos, endPos, color, 1.5f);
            }
        }

        public void DrawSmoothCircle(float radius, uint color, float thickness, int segments = 64)
        {
            var vList = ImGui.GetForegroundDrawList();
            var io = ImGui.GetIO();
            float centerX = io.DisplaySize.X / 2;
            float centerY = io.DisplaySize.Y / 2;

            vList.AddCircle(new Vector2(centerX, centerY), radius, color, segments, thickness);
        }

        public void DrawHealthBarVertical(
    Entity entity,
    Vector2 headScreenPos,
    Vector2 bottomScreenPos,
    float offsetX = 4f,
    float offsetY = 0f,
    bool anchorRight = true,
    float barWidth = 3.5f     
)
        {
            var vList = ImGui.GetForegroundDrawList();

            float maxHealth = entity.Health > 230 ? 500f
                            : entity.Health > 200 ? 230f
                            : 200f;
            float hpPct = Math.Clamp(entity.Health / maxHealth, 0f, 1f);

            float baseHeight = Math.Abs(bottomScreenPos.Y - headScreenPos.Y);
            float headFix = baseHeight * 0.15f;
            float topY = headScreenPos.Y - headFix + offsetY;
            float bottomY = bottomScreenPos.Y + offsetY;      
            float boxHeight = bottomY - topY;

            float boxWidth = baseHeight * 0.5f;
            float leftEdge = headScreenPos.X - boxWidth / 2f;
            float rightEdge = headScreenPos.X + boxWidth / 2f;

            float barX = anchorRight
                ? rightEdge + offsetX
                : leftEdge - barWidth - offsetX;

            vList.AddRectFilled(
                new Vector2(barX, topY),
                new Vector2(barX + barWidth, bottomY),
                ColorToUint32(Color.FromArgb(40, 21, 21, 21))
            );
            Vector4 cG = new Vector4(0f, 1f, 0f, 1f);
            Vector4 cY = new Vector4(1f, 1f, 0f, 1f);
            Vector4 cR = new Vector4(1f, 0f, 0f, 1f);
            Vector4 hpColor = hpPct > 0.7f ? Vector4.Lerp(cY, cG, (hpPct - 0.7f) / 0.3f)
                             : hpPct > 0.4f ? Vector4.Lerp(cR, cY, (hpPct - 0.4f) / 0.3f)
                                             : cR;

            float hpTop = bottomY - boxHeight * hpPct;
            vList.AddRectFilled(
                new Vector2(barX, hpTop),
                new Vector2(barX + barWidth, bottomY),
                ImGui.ColorConvertFloat4ToU32(hpColor)
            );

            vList.AddRect(
                new Vector2(barX, topY),
                new Vector2(barX + barWidth, bottomY),
                ColorToUint32(Color.Gray)
            );
        }

        public static bool Streaming;
        private Dictionary<string, bool> _keyPressStates = new Dictionary<string, bool>();
        private void RenderImgui()
        {
            ImGuiStylePtr style = ImGui.GetStyle();

            style.WindowRounding = 0.0f;
            style.FrameRounding = 0.0f;
            style.GrabRounding = 0.0f;
            style.PopupRounding = 0.0f;
            style.TabRounding = 0.0f;
            style.ScrollbarRounding = 0.0f;

            style.WindowBorderSize = 1.0f;
            style.FrameBorderSize = 0.0f;
            style.ItemSpacing = new Vector2(8, 6);
            style.ItemInnerSpacing = new Vector2(6, 4);
            style.IndentSpacing = 20.0f;
            style.ScrollbarSize = 12.0f;
            style.WindowPadding = new Vector2(10, 10);
            style.FramePadding = new Vector2(8, 5);

            style.Colors[(int)ImGuiCol.Text] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.05f, 0.07f, 0.90f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.07f, 0.07f, 0.09f, 1.00f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.07f, 0.07f, 0.09f, 0.90f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.80f, 0.80f, 0.83f, 0.30f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.92f, 0.91f, 0.88f, 0.00f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.07f, 0.07f, 0.09f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.07f, 0.07f, 0.09f, 1.00f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.80f, 0.80f, 0.83f, 0.31f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.06f, 0.05f, 0.07f, 1.00f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.95f, 0.95f, 0.95f, 1.00f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.18f, 0.18f, 0.21f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.25f, 0.25f, 0.28f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.35f, 0.35f, 0.38f, 1.00f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.06f, 0.05f, 0.07f, 1.00f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.56f, 0.56f, 0.58f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.06f, 0.05f, 0.07f, 1.00f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.07f, 0.07f, 0.09f, 1.00f);
            style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.10f, 0.09f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.07f, 0.07f, 0.09f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.24f, 0.23f, 0.29f, 1.00f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);
            style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.80f, 0.80f, 0.83f, 1.00f);

            ImGui.PushFont(FontManager.VerdanaBig);
            ImGui.End();
            if ((GetAsyncKeyState(0x2D) & 1) != 0)
            {
                show = !show;
            }
            if (!show)
            {
            }
            else
            {
                string windowTitle = " GazanVN  Menu";

                if (AuthHandler.ExpirationDate.HasValue)
                {
                    if (AuthHandler.ExpirationDate.Value > DateTime.UtcNow.AddMonths(3))
                    {
                        windowTitle += " | Time: Lifetime";
                    }
                    else
                    {
                        TimeSpan remainingTime = AuthHandler.ExpirationDate.Value - DateTime.UtcNow;
                        if (remainingTime.TotalHours > 0)
                        {
                            windowTitle += $" | Time: {remainingTime.TotalHours:F0} hours";
                        }
                        else
                        {
                            windowTitle += " | Time: Expired";
                            Environment.Exit(0);
                        }
                    }
                }

                ImGui.SetNextWindowSize(new Vector2(385, 510));
                if (ImGui.Begin(windowTitle, ref show, ImGuiWindowFlags.NoResize))
                {
                    ImGui.Text("Press INSERT to toggle menu");
                    ImGui.Separator();

                    ImGui.Checkbox("ESP Enable", ref Config.ESPEnabled);
                    
                    if (Config.ESPEnabled)
                    {
                        ImGui.Separator();
                        ImGui.Text("ESP Options:");
                        ImGui.Checkbox("ESP Line", ref Config.ESPLineTren);
                        ImGui.SameLine(180);
                        ImGui.Checkbox("ESP Skeleton", ref Config.ESPSkeleton);

                        ImGui.Checkbox("ESP Box", ref Config.ESPBox);
                        ImGui.SameLine(180);
                        ImGui.Checkbox("ESP Box Corner", ref Config.ESPBox2);

                        ImGui.Checkbox("ESP Health", ref Config.ESPHealth);
                        ImGui.SameLine(180);
                        ImGui.Checkbox("ESP Name/Distance", ref Config.ESPName);

                        ImGui.Checkbox("ESP Weapon", ref Config.ESPWeaponIcon);
                        ImGui.SameLine(180);
                        ImGui.Checkbox("Wukong Mode", ref Config.ESPWukong);

                        ImGui.Checkbox("ESP Fill Box", ref Config.ESPFillBox);

                        ImGui.Checkbox("Fix ESP", ref isAutoRefreshActive);
                    }

                    ImGui.Separator();

                    ImGui.Text("Aimbot Settings:");
                    ImGui.Checkbox("Enable Aimbot", ref Config.AimbotV2Enabled);
                    ImGui.SameLine();
                    if (ImGui.Button(Config.ActiveKeybind == KeybindState.Aimbot ? "Press a Key" : Config.AimBotKeyLabel))
                    {
                        Config.ActiveKeybind = (Config.ActiveKeybind == KeybindState.Aimbot) ? KeybindState.None : KeybindState.Aimbot;
                    }
                    //ImGui.Checkbox("Aimbot Mouse", ref Config.AimbotMouseControl);
                    //ImGui.Checkbox("AimLock [ Risk ]", ref Config.AimLock);
                    //ImGui.SliderFloat("Aimbot Delay", ref Config.AimbotDelayHold, 0f, 120f, "%.0f ms");
                    ImGui.Checkbox("FOV Enable", ref Config.FOVMouseEnabled);

                    ImGui.Separator();

                    ImGui.Text("Aimbot Configuration");
                    //ImGui.SliderFloat("Aimbot Smoothness", ref Config.AimbotSmoothness, 1, 9, "%.0f");
                    ImGui.SliderFloat("Aimbot Delay", ref Config.AimbotDelayHold, 00f, 130f, "%.0f ms");
                    ImGui.SliderFloat("FOV Radius", ref Config.FOVMouseRadius, 1f, 200f, "%.0f");
                    ImGui.SliderFloat("Tap Smoothness", ref Config.FOVTapSmoothness, 2f, 5f, "%.1f");
                    ImGui.SliderFloat("Hold Smoothness", ref Config.FOVHoldSmoothness, 10f, 20f, "%.1f");

                    ImGui.Spacing();

                    if (ImGui.Button("Low Smooth"))
                    {
                        Config.FOVTapSmoothness = 2.0f;
                        Config.FOVHoldSmoothness = 10.0f;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Medium Smooth"))
                    {
                        Config.FOVTapSmoothness = 3.5f;
                        Config.FOVHoldSmoothness = 15.0f;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("High Smooth"))
                    {
                        Config.FOVTapSmoothness = 5.0f;
                        Config.FOVHoldSmoothness = 20.0f;
                    }

                    ImGui.Separator();

                    ImGui.Text("Teleport & Pull Options:");
                    ImGui.Checkbox("Pull Enemies", ref Config.PullEnemies);
                    ImGui.SameLine();
                    if (ImGui.Button(Config.ActiveKeybind == KeybindState.Pull ? "Press a Key" : Config.PullKeyLabel))
                    {
                        Config.ActiveKeybind = (Config.ActiveKeybind == KeybindState.Pull) ? KeybindState.None : KeybindState.Pull;
                    }

                    ImGui.SliderFloat("Pull Distance", ref Config.distancepull, 1f, 20f, "%.1f m");

                    ImGui.Checkbox("Teleport", ref Config.TeleportV2);
                    ImGui.SameLine();
                    if (ImGui.Button(Config.ActiveKeybind == KeybindState.Teleport ? "Press a Key" : Config.TeleportKeyLabel))
                    {
                        Config.ActiveKeybind = (Config.ActiveKeybind == KeybindState.Teleport) ? KeybindState.None : KeybindState.Teleport;
                    }

                    ImGui.Separator();

                    ImGui.Text("Visual Settings:");
                    ImGui.Checkbox("No Recoil [ Risk ]", ref Config.NoRecoil);
                    ImGui.Checkbox("No Reload [ Risk ]", ref Config.NoReload);

                    ImGui.Separator();
                    ImGui.Text("Recording Options:");
                    ImGui.Checkbox("Stream Proof", ref Config.StreamMode);
                    ImGui.Checkbox("Show Text", ref Config.ShowText);

                    if (Config.ActiveKeybind != KeybindState.None)
                    {
                        foreach (Keys key in Enum.GetValues(typeof(Keys)))
                        {
                            if (KeyHelper.IsKeyDown(key))
                            {
                                switch (Config.ActiveKeybind)
                                {
                                    case KeybindState.Aimbot:
                                        Config.AimBotKey = key;
                                        Config.AimBotKeyLabel = key.ToString();
                                        break;
                                    case KeybindState.Pull:
                                        Config.PullPlayerKey = key;
                                        Config.PullKeyLabel = key.ToString();
                                        break;
                                    case KeybindState.Teleport:
                                        Config.TeleportKey = key;
                                        Config.TeleportKeyLabel = key.ToString();
                                        break;
                                }
                                Config.ActiveKeybind = KeybindState.None;
                                break;
                            }
                        }
                    }
                }
                ImGui.End();
            }
            ImGui.PopFont();
        }

        private void HandleKeybinds()
        {
            HandleToggle("Aimbot", Config.AimBotKey, ref Config.AimbotV2Enabled);
            HandleToggle("PullEnemies", Config.PullPlayerKey, ref Config.PullEnemies);
            HandleToggle("Teleport", Config.TeleportKey, ref Config.TeleportV2);
        }
        private void HandleToggle(string featureName, Keys key, ref bool featureFlag)
        {
            if (!_keyPressStates.ContainsKey(featureName))
            {
                _keyPressStates[featureName] = false;
            }
            if (key != Keys.None && KeyHelper.IsKeyDown(key))
            {
                if (!_keyPressStates[featureName])
                {
                    featureFlag = !featureFlag;
                    _keyPressStates[featureName] = true;
                }
            }
            else
            {
                _keyPressStates[featureName] = false;
            }
        }

        static uint ColorToUint32(Color color)
        {
            return ImGui.ColorConvertFloat4ToU32(new Vector4(
            (float)(color.R / 255.0),
                (float)(color.G / 255.0),
                (float)(color.B / 255.0),
                (float)(color.A / 255.0)));
        }

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        enum WDA
        {
            WDA_NONE = 0x00000000,
            WDA_MONITOR = 0x00000001,
            WDA_EXCLUDEFROMCAPTURE = 0x00000011,
        }

        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]

        public static extern uint SetWindowDisPlayAffinity(IntPtr hWnd, uint dwReserved);

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        void CreateHandle()
        {
            RECT rect;
            GetWindowRect(Core.Handle, out rect);
            int x = rect.Left;
            int y = rect.Top;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            ImGui.SetWindowSize(new Vector2((float)width, (float)height));
            ImGui.SetWindowPos(new Vector2((float)x, (float)y));
            Size = new Size(width, height);
            Position = new Point(x, y);

            Core.Width = width;
            Core.Height = height;

            string overlay = "Overlay";
            IntPtr OverlayHwnd = FindWindow(null, overlay);

            if (Config.StreamMode)
            {
                int exStyle = GetWindowLong(this.hWnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(this.hWnd, GWL_EXSTYLE, exStyle);

                SetWindowDisplayAffinity(this.hWnd, WDA_EXCLUDEFROMCAPTURE);
                SetWindowDisplayAffinity(OverlayHwnd, (uint)WDA.WDA_EXCLUDEFROMCAPTURE);
            }
            else
            {
                int exStyle = GetWindowLong(this.hWnd, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TOOLWINDOW;
                SetWindowLong(this.hWnd, GWL_EXSTYLE, exStyle);

                SetWindowDisplayAffinity(this.hWnd, 0);
                SetWindowDisplayAffinity(OverlayHwnd, (uint)WDA.WDA_NONE);
            }
            //if (Config.StreamMode)
            //{
            //    SetWindowDisplayAffinity(this.hWnd, WDA_EXCLUDEFROMCAPTURE);
            //}
            //else
            //{
            //    SetWindowDisplayAffinity(this.hWnd, 0);
            //}
        }
    }
}