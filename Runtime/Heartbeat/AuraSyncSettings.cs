using System;
using System.Diagnostics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Configurações do AuraSync
    /// Classe com configurações predefinidas que não podem ser alteradas pelo usuário
    /// </summary>
    [Serializable]
    public class AuraSyncSettings
    {
        // Propriedades somente leitura para evitar modificação externa
        public string User { get; private set; } = "";
        public string ProjectName { get; private set; } = "";
        public string BackendUrl { get; private set; } = "https://ulgebuochosphlsmfmrz.supabase.co/functions/v1/log";
        public string ApiKey { get; private set; } = "aurasync_test_key_1234567890";
        public bool EnableHeartbeats { get; private set; } = true;
        
        /// <summary>
        /// Cria uma nova instância das configurações com valores padrão detectados automaticamente
        /// </summary>
        public static AuraSyncSettings CreateDefault()
        {
            string resolvedUser = ResolveUser();
            var settings = new AuraSyncSettings
            {
                User = resolvedUser,
                ProjectName = Application.productName,
                EnableHeartbeats = true
            };
            
            return settings;
        }
        
        /// <summary>
        /// Retorna as configurações predefinidas
        /// </summary>
        /// <returns>Uma nova instância de AuraSyncSettings com os valores automáticos detectados</returns>
        public static AuraSyncSettings Load()
        {
            // Sempre retorna uma instância nova com os valores atualizados (nome do usuário, project, etc)
            return CreateDefault();
        }

        private static string ResolveUser()
        {
            // Allow manual override through environment variables
            string[] envVars = { "AURASYNC_USER_EMAIL", "AURASYNC_USER", "EMAIL" };
            foreach (var env in envVars)
            {
                var val = Sanitize(Environment.GetEnvironmentVariable(env));
                if (!string.IsNullOrEmpty(val)) return val;
            }

            // Unity account name (may be anonymous when not signed in)
            var unityUser = Sanitize(UnityEditor.CloudProjectSettings.userName);
            if (!string.IsNullOrEmpty(unityUser)) return unityUser;

            // Git configured email
            var gitEmail = Sanitize(GetGitEmail());
            if (!string.IsNullOrEmpty(gitEmail)) return gitEmail;

            // System username fallback (ensure non-empty to avoid rejecting payloads)
            var sysUser = Sanitize(Environment.UserName);
            if (!string.IsNullOrEmpty(sysUser)) return sysUser + "@local";

            // Final fallback
            return "unknown@local";
        }

        private static string GetGitEmail()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "config user.email",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(2000);
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        return output;
                    }
                }
            }
            catch
            {
                // Ignore git lookup failures
            }

            return null;
        }

        private static string Sanitize(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return null;
            candidate = candidate.Trim();
            if (string.Equals(candidate, "anonymous", StringComparison.OrdinalIgnoreCase)) return null;
            return candidate;
        }
    }
}
#endif
