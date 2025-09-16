using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class SilentAimbotV2
    {
        internal static void Work()
        {
            while (true)
            {
                if (!Config.Slient2)
                {
                    Thread.Sleep(0);
                    continue;
                }

                if ((WinAPI.GetAsyncKeyState(Config.Silent1) & 0x8000) == 0)
                {
                    Thread.Sleep(0);
                    continue;
                }

                Entity target = null;
                float distance = float.MaxValue;

                if (Core.Width == -1 || Core.Height == -1) continue;
                if (!Core.HaveMatrix) continue;

                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

                foreach (var entity in Core.Entities.Values)
                {
                    if (entity.IsDead) continue;

                    if (entity.IsKnocked) continue;

                    var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);

                    if (head2D.X < 1 || head2D.Y < 1) continue;

                    var playerDistance = Vector3.Distance(Core.LocalMainCamera, entity.Head);

                    if (playerDistance > Config.AimBotMaxDistance) continue;

                    var x = head2D.X - screenCenter.X;
                    var y = head2D.Y - screenCenter.Y;
                    var crosshairDist = (float)Math.Sqrt(x * x + y * y);

                    if (crosshairDist >= distance || crosshairDist == float.MaxValue)
                    {
                        continue;
                    }

                    if (crosshairDist > 10000)
                    {
                        continue;
                    }

                    distance = crosshairDist;
                    target = entity;
                }

                if (target != null)
                {

                    var firecheck = InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.silentaim, out var firecheck2);

                    if (firecheck2)
                    {
                        var testeee = InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.silent, out var testeee22);
                        if (testeee22 != 0)
                        {
                            InternalMemory.Read<Vector3>(testeee22 + Offsets.silent2, out var StartPosition);
                            InternalMemory.Write(testeee22 + Offsets.silent3, target.Head - StartPosition);
                        }
                    }
                }
            }
        }
    }
}
