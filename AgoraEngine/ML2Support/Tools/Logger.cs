using UnityEngine;
using UnityEngine.UI;

namespace agora_utilities
{
    /// <summary>
    ///   The Logger class provides a simply on screen logging output.
    /// An UI Text is required for writing the output.  If not provided,
    /// the log can only be seen from the system Debug output.
    /// </summary>
    public class Logger
    {
        Text text;

        public Logger(Text text)
        {
            this.text = text;
        }

        public void UpdateLog(string logMessage)
        {
            Debug.Log(logMessage);
            if (text != null)
            {
                string srcLogMessage = text.text;
                if (srcLogMessage.Length > 1000)
                {
                    srcLogMessage = "";
                }
                srcLogMessage += "\r\n \r\n";
                srcLogMessage += logMessage;
                text.text = srcLogMessage;
            }
        }

        public bool DebugAssert(bool condition, string message)
        {
            if (!condition)
            {
                UpdateLog(message);
                return false;
            }
            Debug.Assert(condition, message);
            return true;
        }
    }
}