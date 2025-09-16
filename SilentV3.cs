using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class SilentV3
    {
        internal static void Work()
        {
            while (true)
            {
                if (!Config.Slient)
                {
                    Thread.SpinWait(1);
                    continue;
                }

                Entity target = null;
                float distance = float.MaxValue;

                if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                {
                    Thread.SpinWait(1);
                    continue;
                }
                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                foreach (var entity in Core.Entities.Values)
                {
                    if (entity.IsDead || entity.IsKnocked) continue;
                    var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                    Vector2 headScreenPos2 = new Vector2(head2D.X, head2D.Y);
                    var playerDistance = Vector2.Distance(screenCenter, headScreenPos2);
                    if (playerDistance < distance)
                    {
                        distance = playerDistance;
                        target = entity;
                    }
                }

                if (target != null)
                {
                    var isShooting = InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.sAim1, out var readSuccess);
                    if (readSuccess && isShooting)
                    {
                        var weaponData = InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.sAim2, out var weaponSuccess);
                        if (weaponSuccess != 0)
                        {
                            Vector3 adjustedTauko = target.Head + new Vector3(0, 0.1f, 0);
                            InternalMemory.Read<Vector3>(weaponSuccess + Offsets.sAim3, out var startPos);
                            Vector3 aimPosition = adjustedTauko - startPos;
                            InternalMemory.Write<Vector3>(weaponSuccess + Offsets.sAim4, aimPosition);
                        }
                    }
                }
                Thread.SpinWait(1);
            }
        }
    }
}
