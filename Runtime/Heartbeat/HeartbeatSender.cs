#if (UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Classe responsável por enviar heartbeats para o backend
    /// </summary>
    public class HeartbeatSender
    {
        private readonly IAuraSyncLogger _logger;
        private readonly AuraSyncSettings _settings;
        private readonly Queue<HeartbeatData> _pendingHeartbeats = new Queue<HeartbeatData>();
        private bool _isSending = false;
        
        public HeartbeatSender(AuraSyncSettings settings, IAuraSyncLogger logger = null)
        {
            _settings = settings;
            _logger = logger ?? new DefaultLogger();
        }
        
        /// <summary>
        /// Envia um heartbeat para o backend
        /// </summary>
        public void SendHeartbeat(HeartbeatData heartbeatData)
        {
            if (heartbeatData == null)
                return;
                
            _pendingHeartbeats.Enqueue(heartbeatData);
            
            if (!_isSending)
            {
                _isSending = true;
                ProcessQueueAsync();
            }
        }
        
        private async void ProcessQueueAsync()
        {
            try
            {
                while (_pendingHeartbeats.Count > 0)
                {
                    HeartbeatData heartbeatData = _pendingHeartbeats.Dequeue();
                    await SendHeartbeatToBackendAsync(heartbeatData);
                    
                    // Pequeno atraso para não sobrecarregar o backend
                    await Task.Delay(100);
                }
            }
            catch (System.Exception ex)
            {
                // Silent fail - only log in debug mode
#if AURA_SYNC_DEBUG
                _logger.LogWarning($"Error processing heartbeat queue: {ex.Message}");
#endif
            }
            finally
            {
                _isSending = false;
            }
        }
        
        private async Task SendHeartbeatToBackendAsync(HeartbeatData heartbeatData)
        {
            if (string.IsNullOrEmpty(_settings.BackendUrl) || string.IsNullOrEmpty(_settings.ApiKey))
            {
#if AURA_SYNC_DEBUG
                _logger.LogWarning("Backend URL or API Key is not configured. Heartbeat not sent.");
#endif
                return;
            }
            
            try
            {
                // Criar o payload completo
                DevActivityPayload payload = CreatePayload(heartbeatData);
                string jsonPayload = JsonUtility.ToJson(payload);

#if AURA_SYNC_DEBUG
                // Log do JSON formatado para melhor legibilidade (somente em modo de depuração)
                _logger.Log($"JSON a ser enviado: \n{FormatJson(jsonPayload)}");
#endif
                
                using (UnityWebRequest request = new UnityWebRequest(_settings.BackendUrl, "POST"))
                {
                    try
                    {
                        // Desabilitar encoding UTF8 e usar diretamente o JSON como string
                        byte[] bodyRaw = System.Text.Encoding.Default.GetBytes(jsonPayload);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        //request.SetRequestHeader("Authorization", $"Bearer {_settings.ApiKey}");
                        request.SetRequestHeader("api_key", _settings.ApiKey);

                        // Definir um timeout para a requisição (10 segundos)
                        request.timeout = 10;
                        
                        // Enviar requisição
                        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                        
                        // Aguardar conclusão com timeout de segurança
                        int timeoutCounter = 0;
                        int maxTimeout = 120; // 12 segundos total (120 * 100ms)
                        
                        while (!operation.isDone)
                        {
                            await Task.Delay(100);
                            timeoutCounter++;
                            
                            if (timeoutCounter >= maxTimeout)
                            {
#if AURA_SYNC_DEBUG
                                _logger.LogWarning("Request timeout exceeded. Network may be unavailable.");
#endif
                                request.Abort();
                                break;
                            }
                        }
                        
                        // Verificar resultado
                        if (request.result != UnityWebRequest.Result.Success)
                        {
#if AURA_SYNC_DEBUG
                            // Tratamento específico para problemas de conectividade
                            if (request.result == UnityWebRequest.Result.ConnectionError)
                            {
                                _logger.LogWarning("Network connection error. Unable to send heartbeat. Will retry later.");
                            }
                            else
                            {
                                _logger.LogWarning($"Error sending heartbeat: {request.error} (status: {request.responseCode})");
                                _logger.LogWarning($"Response: {request.downloadHandler?.text ?? "No response"}");
                            }
#endif
                        }
                        else
                        {
#if AURA_SYNC_DEBUG
                            _logger.Log($"Heartbeat sent successfully: {request.responseCode} {request.downloadHandler.text}");
#endif
                        }
                    }
                    catch (System.Exception ex)
                    {
#if AURA_SYNC_DEBUG
                        _logger.LogWarning($"Exception during web request: {ex.Message}");
#endif
                    }
                    finally
                    {
                        // Garantir que os handlers sejam liberados para evitar vazamentos de memória
                        request.uploadHandler?.Dispose();
                        request.downloadHandler?.Dispose();
                    }
                }
            }
            catch (System.Exception ex)
            {
#if AURA_SYNC_DEBUG
                _logger.LogWarning($"Failed to send heartbeat: {ex.Message}");
#endif
            }
        }
        
        private DevActivityPayload CreatePayload(HeartbeatData heartbeatData)
        {
            return new DevActivityPayload
            {
                user = _settings.User,
                heartbeat_data = heartbeatData
            };
        }
        
        /// <summary>
        /// Formata uma string JSON para melhorar a legibilidade
        /// </summary>
        private string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;
                
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;
            int indentLevel = 0;
            
            foreach (char c in json)
            {
                if (c == '\"' && (sb.Length == 0 || sb[sb.Length - 1] != '\\'))
                    inQuotes = !inQuotes;
                    
                if (!inQuotes)
                {
                    if (c == '{' || c == '[')
                    {
                        sb.Append(c);
                        sb.Append('\n');
                        indentLevel++;
                        sb.Append(new string(' ', indentLevel * 4));
                        continue;
                    }
                    
                    if (c == '}' || c == ']')
                    {
                        sb.Append('\n');
                        indentLevel--;
                        sb.Append(new string(' ', indentLevel * 4));
                        sb.Append(c);
                        continue;
                    }
                    
                    if (c == ',')
                    {
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indentLevel * 4));
                        continue;
                    }
                    
                    if (c == ':')
                    {
                        sb.Append(c);
                        sb.Append(' ');
                        continue;
                    }
                }
                
                sb.Append(c);
            }
            
            return sb.ToString();
        }
    }
}

#endif
