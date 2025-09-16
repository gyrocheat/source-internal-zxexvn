using System;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace x
{
    public class AuthRequest
    {
        public string key { get; set; }
        public string hwid { get; set; }
    }

    public class AuthResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public DateTime? expires { get; set; }
    }
    public static class AuthHandler
    {
        public static DateTime? ExpirationDate { get; set; }
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "user.xzex");

        public static async Task<AuthResponse> ValidateKeyAsync(string userKey)
        {
            string apiUrl = "http://103.157.205.156:3000/api/validate";
            string responseString = string.Empty;
            string jsonPayload = string.Empty;

            try
            {
                File.Delete(logFilePath);
                WriteLog("--- Bat dau phien xac thuc moi ---");

                string hardwareId = GetHardwareId();
                WriteLog($"Da tao HWID: {hardwareId}");

                jsonPayload = $"{{\"key\":\"{userKey}\",\"hwid\":\"{hardwareId}\"}}";
                WriteLog($"Da tao JSON Payload: {jsonPayload}");

                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                WriteLog("Da tao HttpContent. Chuan bi gui yeu cau...");

                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                WriteLog($"Da gui yeu cau. Server phan hoi voi ma: {(int)response.StatusCode}");

                responseString = await response.Content.ReadAsStringAsync();
                WriteLog($"Noi dung phan hoi tho tu server:\n{responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    return new AuthResponse { success = false, message = $"Lỗi từ server (HTTP {(int)response.StatusCode})." };
                }

                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    JsonElement root = doc.RootElement;
                    var result = new AuthResponse();
                    if (root.TryGetProperty("success", out JsonElement successElement)) result.success = successElement.GetBoolean();
                    if (root.TryGetProperty("message", out JsonElement messageElement)) result.message = messageElement.GetString();
                    if (root.TryGetProperty("expires", out JsonElement expiresElement) && expiresElement.ValueKind != JsonValueKind.Null) result.expires = expiresElement.GetDateTime();
                    WriteLog("Phan tich cu phap phan hoi thanh cong.");
                    return result;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"!!! LOI NGOAI LE NGHIEM TRONG: {ex.GetType().Name} - {ex.Message}");
                WriteLog(ex.StackTrace);
                return new AuthResponse { success = false, message = $"Lỗi hệ thống: {ex.Message}. Chi tiet trong file log tren Desktop." };
            }
        }

        private static void WriteLog(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        public static string GetHardwareId()
        {
            try
            {
                string processorId = GetWmicInfo("cpu", "ProcessorId");

                if (string.IsNullOrWhiteSpace(processorId))
                {
                    WriteLog("KHONG THE LAY THONG TIN ProcessorId TU WMIC.EXE.");
                    return "HWID_WMI_FAILED";
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(processorId));
                    var builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Loi khi tao HWID: {ex.Message}");
                return "UNABLE_TO_RETRIEVE_HWID";
            }
        }
        private static string GetWmicInfo(string wmiClass, string wmiProperty)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = $"{wmiClass} get {wmiProperty}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var lines = output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    return lines[1].Trim();
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Loi khi chay wmic.exe cho '{wmiClass}.{wmiProperty}': {ex.Message}");
            }
            return string.Empty;
        }
    }
}
