#if (UNITY_EDITOR)

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Heimo.AuraSync.Heartbeat
{
    // A interface ILogger foi movida para AuraSyncLogger.cs
    
    /// <summary>
    /// Interface para clientes Git
    /// </summary>
    public interface IGitClient
    {
        string GetBranchName(string workingDir);
    }
    
    /// <summary>
    /// Cliente Git que usa linha de comando
    /// </summary>
    public class GitClient : IGitClient
    {
        private readonly IAuraSyncLogger _logger;
        
        public GitClient(IAuraSyncLogger logger = null)
        {
            _logger = logger ?? new DefaultLogger();
        }
        
        public string GetBranchName(string workingDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(workingDir))
                    return null;
                    
                // Verificar se o diretório é um repositório Git
                if (!Directory.Exists(Path.Combine(workingDir, ".git")))
                {
                    // Tente verificar diretórios superiores até encontrar um .git
                    DirectoryInfo dir = new DirectoryInfo(workingDir);
                    while (dir != null)
                    {
                        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                        {
                            workingDir = dir.FullName;
                            break;
                        }
                        dir = dir.Parent;
                    }
                    
                    // Se não encontrou .git em nenhum diretório pai, retorne null
                    if (dir == null)
                        return null;
                }
                
                // Executar comando git para obter o nome do branch atual
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDir
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    string error = process.StandardError.ReadToEnd().Trim();
                    
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        return output;
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"Git error: {error}");
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting git branch: {ex.Message}");
                return null;
            }
        }
    }
}

#endif
