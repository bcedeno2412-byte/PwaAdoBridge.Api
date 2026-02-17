using System;
using System.Text;

namespace PwaAdoBridge.Api.Services
{
    /// <summary>
    /// Utility service for decoding Base64 strings
    /// </summary>
    public static class SettingsService
    {
        /// <summary>
        /// Decodes a Base64 string. 
        /// If input is null, empty, or invalid Base64, returns the original input.
        /// </summary>
        public static string Base64Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                var bytes = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Return original input if it's not valid Base64
                return input;
            }
        }
    }
}
