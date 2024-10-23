using System.Text;

namespace EldenRingInfiniteNGPlus;

public class LogFile
{
    public bool Debug { get; set; }
    
    FileStream stream;
    
    public LogFile(string path)
    {
        stream = new FileStream(path, FileMode.Create);
    }

    public void Log(string msg, bool newline = true)
    {
        // TODO: Prepend timestamp.
        // TODO: Encode msg.
        stream.Write(newline ? msg + "\n" : msg);
    }

    public void LogDebug(string msg, bool newline = true)
    {
        if (!Debug) return;

        // TODO: Prepend timestamp and 'DEBUG'.
        // TODO: Encode msg.
        stream.Write(newline ? msg + "\n" : msg);
    }
}