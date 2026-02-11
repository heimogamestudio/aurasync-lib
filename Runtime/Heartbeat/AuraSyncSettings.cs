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
        private const string DefaultBackendUrl = "https://web-production-6f5bc.up.railway.app/api/pulse/heartbeat";
        private const string DefaultApiKey = "EZ96C7VQA4FFNzumwjIpvby7cWYqRXlK";
        private const string DefaultOrganizationId = "7OmJOSlFwFc37QisZiGSjPX6QwgHE0A7";

        // Propriedades somente leitura para evitar modificação externa
        public string User { get; private set; } = "";
        public string ProjectName { get; private set; } = "";
        public string BackendUrl { get; private set; } = DefaultBackendUrl;
        public string ApiKey { get; private set; } = DefaultApiKey;
        public string OrganizationId { get; private set; } = DefaultOrganizationId;
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
                BackendUrl = ResolveBackendUrl(),
                ApiKey = ResolveApiKey(),
                OrganizationId = ResolveOrganizationId(),
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

        private static string ResolveBackendUrl()
        {
            var envUrl = Sanitize(Environment.GetEnvironmentVariable("AURASYNC_BACKEND_URL"));
            if (!string.IsNullOrEmpty(envUrl)) return envUrl;
            return DefaultBackendUrl;
        }

        private static string ResolveApiKey()
        {
            var envKey = Sanitize(Environment.GetEnvironmentVariable("AURASYNC_API_KEY"));
            if (!string.IsNullOrEmpty(envKey)) return envKey;
            return DefaultApiKey;
        }

        private static string ResolveOrganizationId()
        {
            var envOrgId = Sanitize(Environment.GetEnvironmentVariable("AURASYNC_ORGANIZATION_ID"));
            if (!string.IsNullOrEmpty(envOrgId)) return envOrgId;
            return DefaultOrganizationId;
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
