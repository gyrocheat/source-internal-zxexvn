using AotForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static AotForms.WinAPI;

namespace Client
{
    internal static class FOV
    {
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_LBUTTON = 0x01;
        private const float SHAKE_THRESHOLD = 0.5f;

        private static Entity currentTarget = null;
        private static Vector2 mouseMoveRemainder = Vector2.Zero;

        private static readonly Stopwatch lButtonHoldTimer = new Stopwatch();

        public static void Work()
        {
            new Thread(Loop) { IsBackground = true }.Start();
        }

        private static void Loop()
        {
            while (true)
            {
                bool isLButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

                if (isLButtonDown)
                {
                    if (!lButtonHoldTimer.IsRunning)
                    {
                        lButtonHoldTimer.Restart();
                    }
                }
                else
                {
                    if (lButtonHoldTimer.IsRunning)
                    {
                        lButtonHoldTimer.Stop();
                    }
                }

                if (!ShouldAim(isLButtonDown))
                {
                    currentTarget = null;
                    mouseMoveRemainder = Vector2.Zero;
                    Thread.Sleep(100);
                    continue;
                }

                if (!IsValidTarget(currentTarget) || !IsInFov(currentTarget))
                {
                    currentTarget = FindClosestEnemyInFov();
                    mouseMoveRemainder = Vector2.Zero;
                }

                if (currentTarget != null)
                {
                    AimSmoothlyAt(currentTarget);
                }

                Thread.Sleep(5);
            }
        }

        private static bool ShouldAim(bool isLButtonDown)
        {
            return Config.FOVMouseEnabled &&
                   isLButtonDown &&
                   Core.HaveMatrix &&
                   Core.Width > 0 &&
                   Core.Height > 0;
        }

        private static bool IsValidTarget(Entity entity)
        {
            return entity != null &&
                   !entity.IsDead &&
                   entity.Address != 0 &&
                   Core.Entities.ContainsKey(entity.Address);
        }

        private static bool IsInFov(Entity entity)
        {
            if (entity == null) return false;
            Vector2 center = new(Core.Width / 2f, Core.Height / 2f);
            Vector2 headScreen = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
            if (headScreen.X <= 1 || headScreen.Y <= 1) return false;
            float dist = Vector2.Distance(center, headScreen);
            float stickyFov = Config.FOVMouseRadius * 1.2f;
            return dist < (currentTarget == entity ? stickyFov : Config.FOVMouseRadius);
        }

        private static Entity FindClosestEnemyInFov()
        {
            Entity bestTarget = null;
            float closestDistance = Config.FOVMouseRadius;
            Vector2 screenCenter = new(Core.Width / 2f, Core.Height / 2f);
            foreach (var entity in Core.Entities.Values)
            {
                if (!IsValidTarget(entity) || (Config.IgnoreKnocked && entity.IsKnocked)) continue;
                Vector2 headScreen = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (headScreen.X <= 1 || headScreen.Y <= 1) continue;
                float dist = Vector2.Distance(screenCenter, headScreen);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }

        private static void AimSmoothlyAt(Entity target)
        {
            const long tapThresholdMs = 150;
            float smoothFactor;

            if (lButtonHoldTimer.ElapsedMilliseconds < tapThresholdMs)
            {
                smoothFactor = Config.FOVTapSmoothness;
            }
            else 
            {
                smoothFactor = Config.FOVHoldSmoothness;
            }

            Vector2 screenCenter = new(Core.Width / 2f, Core.Height / 2f);
            Vector2 headScreen = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            Vector2 targetVector = headScreen - screenCenter;

            if (targetVector.Length() < SHAKE_THRESHOLD)
            {
                mouseMoveRemainder = Vector2.Zero;
                return;
            }

            Vector2 moveDelta = targetVector / Math.Max(1.0f, smoothFactor);
            Vector2 totalMove = moveDelta + mouseMoveRemainder;

            int moveX = (int)totalMove.X;
            int moveY = (int)totalMove.Y;

            mouseMoveRemainder = new Vector2(totalMove.X - moveX, totalMove.Y - moveY);

            if (moveX != 0 || moveY != 0)
            {
                MoveMouse(moveX, moveY);
            }
        }

        private static void MoveMouse(int dx, int dy)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = 0;
            input[0].mi = new MOUSEINPUT
            {
                dx = dx,
                dy = dy,
                mouseData = 0,
                dwFlags = 0x0001,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };
            SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT { public uint type; public MOUSEINPUT mi; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    }
}
