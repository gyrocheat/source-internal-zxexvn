using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class WeaponIconManager
    {
        public delegate void ImageLoaderDelegate(string path, bool v, out IntPtr handle, out int width, out int height);

        private static readonly Dictionary<string, IntPtr> _weaponIconHandles = new Dictionary<string, IntPtr>();

        public static void LoadAllWeaponIcons(string iconDirectory, ImageLoaderDelegate imageLoader)
        {
            if (_weaponIconHandles.Count > 0) return; // Nếu đã tải rồi thì bỏ qua.

            if (!Directory.Exists(iconDirectory))
            {
                Console.WriteLine($"[WeaponIconManager] Lỗi: Không tìm thấy thư mục tại '{iconDirectory}'");
                return;
            }

            var iconFiles = Directory.GetFiles(iconDirectory, "*.png");

            foreach (var imagePath in iconFiles)
            {
                var weaponName = Path.GetFileNameWithoutExtension(imagePath).ToLower();

                try
                {
                    imageLoader(imagePath, true, out IntPtr imageHandle, out _, out _);

                    if (imageHandle != IntPtr.Zero && !_weaponIconHandles.ContainsKey(weaponName))
                    {
                        _weaponIconHandles.Add(weaponName, imageHandle);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WeaponIconManager] Không thể tải icon '{imagePath}'. Lỗi: {ex.Message}");
                }
            }
            Console.WriteLine($"[WeaponIconManager] Đã tải thành công {_weaponIconHandles.Count} icon vũ khí.");
        }

        public static IntPtr GetIcon(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName))
                return IntPtr.Zero;

            _weaponIconHandles.TryGetValue(weaponName.ToLower(), out IntPtr handle);
            return handle;
        }
    }
}
