using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class TeleportV3
    {
        private static Task tele;
        private static CancellationTokenSource cts = new();
        private static bool isRunning = false;

        private static HashSet<uint> teleportedEnemies = new();

        internal static void Work()
        {
            if (isRunning) return;

            cts = new CancellationTokenSource();
            isRunning = true;

            tele = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Config.TeleportV3)
                    {
                        teleportedEnemies.Clear();
                        await Task.Delay(100, cts.Token);
                        continue;
                    }

                    var targetEnemy = Core.Entities.Values
                        .Where(entity => !entity.IsDead
                                         && (!Config.IgnoreKnocked || !entity.IsKnocked)
                                         && Vector3.Distance(Core.LocalMainCamera, entity.Head) <= 500
                                         && !teleportedEnemies.Contains(entity.Address))
                        .OrderBy(e => Vector3.Distance(Core.LocalMainCamera, e.Head))
                        .FirstOrDefault();

                    if (targetEnemy != null)
                    {
                        var localRootBone = InternalMemory.Read<uint>(Core.LocalPlayer + (uint)Bones.Root, out var localRootBonePtr);
                        var localTransform = InternalMemory.Read<uint>(localRootBonePtr + 0x8, out var localTransformValue);
                        var localTransformObj = InternalMemory.Read<uint>(localTransformValue + 0x8, out var localTransformObjPtr);
                        var localMatrix = InternalMemory.Read<uint>(localTransformObjPtr + 0x20, out var localMatrixValue);
                        var playerPositionPtr = localMatrixValue + 0x80;

                        var enemyRootBone = InternalMemory.Read<uint>(targetEnemy.Address + (uint)Bones.Root, out var enemyRootBonePtr);

                        if (Transform.GetNodePosition(enemyRootBonePtr, out var enemyPosition))
                        {
                            InternalMemory.Read<Vector3>(playerPositionPtr, out var playerCurrentPosition);

                            if (Vector3.Distance(playerCurrentPosition, enemyPosition) > 2.5f)
                            {
                                var direction = Vector3.Normalize(enemyPosition - Core.LocalMainCamera);
                                if (direction == Vector3.Zero)
                                    direction = new Vector3(1, 0, 0);

                                var teleportPos = enemyPosition - direction * Config.TeleportOffset;

                                InternalMemory.Write<Vector3>(playerPositionPtr, teleportPos);

                                teleportedEnemies.Add(targetEnemy.Address);
                            }
                        }
                    }

                    await Task.Delay(150, cts.Token);
                }
            }, cts.Token);
        }
        internal async static void Stop()
        {
            if (!isRunning || tele == null) return;

            try
            {
                cts.Cancel();
                await tele;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                isRunning = false;
                teleportedEnemies.Clear();
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
        }
    }
}
