using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class PullPlayerV2
    {
        private static Entity cachedTarget = null;
        private static DateTime lastTargetUpdate = DateTime.MinValue;

        private static Entity? GetCachedTarget()
        {
            try
            {
                if ((DateTime.Now - lastTargetUpdate).TotalMilliseconds > 200)
                {
                    cachedTarget = FindBestTarget();
                    lastTargetUpdate = DateTime.Now;
                }
                return cachedTarget;
            }
            catch (Exception ex)
            {
                LogError(nameof(GetCachedTarget), ex);
                return null;
            }
        }

        private static Entity FindBestTarget()
        {
            try
            {
                Entity? bestTarget = null;
                float bestDistance = float.MaxValue;
                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

                foreach (var entity in Core.Entities.Values)
                {
                    if (!entity.IsKnown) continue;
                    if (entity.IsDead) continue;
                    if (Config.IgnoreKnocked && entity.IsKnocked) continue;

                    var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                    float playerDistance = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                    if (playerDistance > 10) continue;

                    var crosshairDistance = Vector2.Distance(screenCenter, head2D);
                    if (crosshairDistance < bestDistance)
                    {
                        bestDistance = crosshairDistance;
                        bestTarget = entity;
                    }
                }
                return bestTarget;
            }
            catch (Exception ex)
            {
                LogError(nameof(FindBestTarget), ex);
                return null;
            }
        }

        private static Vector3 GetCameraForward() { return Vector3.Normalize(new Vector3(Core.CameraMatrix.M13, Core.CameraMatrix.M23, Core.CameraMatrix.M33)); }
        private static Vector3 GetCameraRight() { Vector3 forward = GetCameraForward(); Vector3 up = new Vector3(Core.CameraMatrix.M12, Core.CameraMatrix.M22, Core.CameraMatrix.M32); return Vector3.Normalize(Vector3.Cross(up, forward)); }

        internal static void Work()
        {
            while (true)
            {
                try
                {
                    if (!Config.PullEnemiesV2 || Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    Entity target = GetCachedTarget();
                    if (target != null && target.Address != 0)
                    {
                        Vector3 forward = GetCameraForward();
                        Vector3 right = GetCameraRight();
                        Vector3 playerPosition = Core.playerpos;

                        float distance = 0.8f;            // Khoảng cách kéo địch ra trước mặt
                        float horizontalOffset = 0.4f;    // Hiệu chỉnh sang phải, có thể thử nghiệm giá trị này
                        Vector3 newPosition = playerPosition + forward * distance + right * horizontalOffset;
                        uint EntityRootBone;
                        if (!InternalMemory.Read<uint>(target.Address + (uint)Bones.Root, out EntityRootBone) || EntityRootBone == 0)
                            continue;
                        uint transformValue;
                        if (!InternalMemory.Read<uint>(EntityRootBone + 0x8, out transformValue) || transformValue == 0)
                            continue;
                        uint rootBoneclass;
                        if (!InternalMemory.Read<uint>(transformValue + 0x8, out rootBoneclass) || rootBoneclass == 0)
                            continue;
                        uint roootmatrixValuelist;
                        if (!InternalMemory.Read<uint>(rootBoneclass + 0x20, out roootmatrixValuelist) || roootmatrixValuelist == 0)
                            continue;
                        InternalMemory.Write<Vector3>(roootmatrixValuelist + 0x80, newPosition);
                    }
                }
                catch (Exception ex)
                {
                    LogError(nameof(Work), ex);
                }

                Thread.Sleep(5);
            }
        }
        private static void LogError(string methodName, Exception ex)
        {
            try
            {
                string logDir = @"C:\Windows\Temp";
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, "pull_error_log.txt");
                string log = $"[ERROR] {DateTime.Now} - Method: {methodName} - Exception: {ex.Message}\r\n{ex.StackTrace}\r\n\r\n";

                Console.WriteLine(log);
                File.AppendAllText(logPath, log);
            }
            catch { }
        }
    }
}
