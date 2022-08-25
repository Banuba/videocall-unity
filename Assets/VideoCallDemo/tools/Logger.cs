using UnityEngine;
using UnityEngine.UI;

public class Logger
{
    private Text _text;

    public Logger(Text text)
    {
        _text = text;
    }

    public void UpdateLog(string logMessage) 
    {
        Debug.Log(logMessage);
        string srcLogMessage = _text.text;
        if (srcLogMessage.Length > 1000) {
            srcLogMessage = "";
        }
        srcLogMessage += "\r\n \r\n";
        srcLogMessage += logMessage;
        _text.text = srcLogMessage;
    }

    public bool DebugAssert(bool condition, string message)
    {
        if (!condition) {
            UpdateLog(message);
            return false;
        }
        Debug.Assert(condition, message);
        return true;
    }
}