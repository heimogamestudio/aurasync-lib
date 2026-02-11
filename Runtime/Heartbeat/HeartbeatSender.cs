#if (UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Classe responsável por enviar heartbeats para o backend
    /// Usa EditorApplication.update para polling, garantindo execução na main thread
    /// </summary>
    public class HeartbeatSender
    {
        private readonly IAuraSyncLogger _logger;
        private readonly AuraSyncSettings _settings;
        private readonly Queue<HeartbeatData> _pendingHeartbeats = new Queue<HeartbeatData>();
        private UnityWebRequest _activeRequest;
        private bool _isProcessing = false;

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

            if (!_isProcessing)
            {
                _isProcessing = true;
                EditorApplication.update += ProcessQueue;
            }
        }

        private void ProcessQueue()
        {
            // Se tem request ativo, esperar ele terminar
            if (_activeRequest != null)
            {
                if (!_activeRequest.isDone)
                    return;

                HandleResponse(_activeRequest);
                _activeRequest.uploadHandler?.Dispose();
                _activeRequest.downloadHandler?.Dispose();
                _activeRequest.Dispose();
                _activeRequest = null;
            }

            // Processar próximo da fila
            if (_pendingHeartbeats.Count > 0)
            {
                HeartbeatData heartbeatData = _pendingHeartbeats.Dequeue();
                SendRequest(heartbeatData);
            }
            else
            {
                // Fila vazia, parar de processar
                _isProcessing = false;
                EditorApplication.update -= ProcessQueue;
            }
        }

        private void SendRequest(HeartbeatData heartbeatData)
        {
            if (string.IsNullOrEmpty(_settings.BackendUrl) || string.IsNullOrEmpty(_settings.ApiKey))
            {
                _logger.LogWarning("Backend URL or API Key is not configured. Heartbeat not sent.");
                return;
            }

            try
            {
                DevActivityPayload payload = CreatePayload(heartbeatData);
                string jsonPayload = JsonUtility.ToJson(payload);

#if AURA_SYNC_DEBUG
                _logger.Log($"Sending heartbeat to {_settings.BackendUrl}\n{FormatJson(jsonPayload)}");
#endif

                _activeRequest = new UnityWebRequest(_settings.BackendUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                _activeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                _activeRequest.downloadHandler = new DownloadHandlerBuffer();
                _activeRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                _activeRequest.SetRequestHeader("x-api-key", _settings.ApiKey);
                if (!string.IsNullOrEmpty(_settings.OrganizationId))
                    _activeRequest.SetRequestHeader("x-organization-id", _settings.OrganizationId);
                _activeRequest.timeout = 10;

                _activeRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to create heartbeat request: {ex.Message}");
                _activeRequest?.Dispose();
                _activeRequest = null;
            }
        }

        private void HandleResponse(UnityWebRequest request)
        {
            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.result == UnityWebRequest.Result.ConnectionError)
                    {
                        _logger.LogWarning($"Connection error sending heartbeat to {_settings.BackendUrl}: {request.error}");
                    }
                    else
                    {
                        _logger.LogWarning($"Error sending heartbeat: {request.error} (status: {request.responseCode})");
                        _logger.LogWarning($"Response: {request.downloadHandler?.text ?? "No response"}");
                    }
                }
                else
                {
#if AURA_SYNC_DEBUG
                    _logger.Log($"Heartbeat sent successfully: {request.responseCode} {request.downloadHandler.text}");
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error handling heartbeat response: {ex.Message}");
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

#if AURA_SYNC_DEBUG
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
#endif
    }
}

#endif
