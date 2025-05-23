#if (UNITY_EDITOR)

using System;
using System.ComponentModel;
using System.Reflection;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Extensões para trabalhar com enumerações
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Obtém a descrição de um valor de enumeração usando o atributo Description
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            
            if (field == null)
                return value.ToString().ToLowerInvariant();
                
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(
                field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                
            return attribute == null ? value.ToString().ToLowerInvariant() : attribute.Description;
        }
    }
    
    /// <summary>
    /// Extensões para DateTime
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converte DateTime para timestamp Unix (segundos desde 01/01/1970)
        /// </summary>
        public static double ToUnixTimeFloat(this DateTime dateTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (dateTime.ToUniversalTime() - epoch).TotalSeconds;
        }
        
        public static string ToIso8601String(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }  
    }
}

#endif
