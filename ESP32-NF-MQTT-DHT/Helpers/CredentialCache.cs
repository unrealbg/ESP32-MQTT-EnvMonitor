namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    public static class CredentialCache
    {
        // Use a different file path that's likely to be writable on ESP32
        private const string CredentialsPath = "I:\\config\\credentials.txt";
        private static readonly object _syncLock = new object();

        // Buffer size for file operations
        private const int BufferSize = 256;

        public static string Username { get; set; } = "admin";
        
        public static string PasswordHash { get; set; } = "admin";
        
        public static void Load()
        {
            try
            {
                if (!File.Exists(CredentialsPath))
                {
                    Debug.WriteLine("Credentials file not found, using defaults");
                    SetDefaultCredentials();
                    return;
                }

                string[] lines = ReadAllLines(CredentialsPath);
                if (lines.Length >= 2 && !string.IsNullOrEmpty(lines[0]) && !string.IsNullOrEmpty(lines[1]))
                {
                    Username = lines[0].Trim();
                    PasswordHash = lines[1].Trim();
                    Debug.WriteLine("Credentials loaded successfully");
                }
                else
                {
                    SetDefaultCredentials();
                    Debug.WriteLine("Invalid credentials format, using defaults");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading credentials: " + ex.Message);
                LogHelper.LogError("Failed to load credentials", ex);
                SetDefaultCredentials();
            }
        }

        public static void Update(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.WriteLine("Cannot update with empty credentials");
                return;
            }

            lock (_syncLock)
            {
                try
                {
                    // Validate inputs before updating
                    if (username.Length > 50 || password.Length > 100)
                    {
                        Debug.WriteLine("Credentials too long, rejecting update");
                        return;
                    }

                    // Update credentials in memory first
                    Username = username;
                    PasswordHash = password;
                    Debug.WriteLine("Credentials updated in memory");
                    
                    // Try to persist to file
                    SaveCredentialsToFile(username, password);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error updating credentials: " + ex.Message);
                    LogHelper.LogError("Failed to update credentials", ex);
                    // Don't rethrow - we've already updated the credentials in memory
                }
            }
        }

        public static bool Validate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.WriteLine("Credential validation: Empty credentials provided");
                return false;
            }

            bool isValid = Username == username && PasswordHash == password;
            Debug.WriteLine("Credential validation: " + (isValid ? "Success" : "Failed"));
            return isValid;
        }

        private static void SetDefaultCredentials()
        {
            Username = "admin";
            PasswordHash = "admin";
        }

        private static void SaveCredentialsToFile(string username, string password)
        {
            try
            {
                // Try to create the directory if it doesn't exist
                string directory = Path.GetDirectoryName(CredentialsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.WriteLine("Created directory: " + directory);
                }

                // Create credentials content
                var contentBuilder = new StringBuilder();
                contentBuilder.Append(username);
                contentBuilder.Append('\n');
                contentBuilder.Append(password);

                // Write atomically to avoid corruption
                string tempPath = CredentialsPath + ".tmp";
                byte[] content = Encoding.UTF8.GetBytes(contentBuilder.ToString());
                
                File.WriteAllBytes(tempPath, content);
                
                // Atomic move (if supported by filesystem)
                if (File.Exists(CredentialsPath))
                {
                    File.Delete(CredentialsPath);
                }
                
                // Simple rename since atomic move may not be available
                File.Move(tempPath, CredentialsPath);
                
                Debug.WriteLine("Credentials saved to file successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving credentials to file: " + ex.Message);
                LogHelper.LogWarning("Could not persist credentials to file: " + ex.Message);
                
                // Clean up temp file if it exists
                try
                {
                    string tempPath = CredentialsPath + ".tmp";
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private static string[] ReadAllLines(string path)
        {
            var lines = new ArrayList();
            byte[] buffer = null;
            
            try
            {
                buffer = File.ReadAllBytes(path);
                string content = new string(Encoding.UTF8.GetChars(buffer));
                
                // Custom string replacement for \r\n to \n since Replace() is not available in nanoFramework
                string normalizedContent = ReplaceCarriageReturns(content);
                string[] lineArray = normalizedContent.Split('\n');
                
                foreach (string line in lineArray)
                {
                    string trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        lines.Add(trimmedLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading credentials file: " + ex.Message);
                LogHelper.LogError("Failed to read credentials file", ex);
                return new string[0];
            }
            finally
            {
                // Clear sensitive data from memory
                if (buffer != null)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }

            // Convert ArrayList to string array
            var result = new string[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                result[i] = (string)lines[i];
            }
            
            return result;
        }

        // Custom implementation of string replace for \r\n to \n since Replace() is not available in nanoFramework
        private static string ReplaceCarriageReturns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                {
                    // Skip the \r, the \n will be added in the next iteration
                    continue;
                }
                else
                {
                    sb.Append(input[i]);
                }
            }
            
            return sb.ToString();
        }
    }
}
