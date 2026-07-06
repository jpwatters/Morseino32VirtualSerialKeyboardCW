using System;

namespace MorserinoWinKeyboard
{
    /// <summary>
    /// Common interface for anything the app can send typed text through --
    /// currently just a USB serial connection to a chosen COM port (see
    /// SerialKeyboard.cs). MainForm talks to whichever transport is active
    /// through this interface so it doesn't need to know which concrete
    /// class it's using. This mirrors the Mac app's KeyboardTransport
    /// protocol (KeyboardTransport.h) exactly.
    /// </summary>
    public interface IKeyboardTransport
    {
        bool IsConnected { get; }
        string ConnectedDeviceName { get; }

        void SendString(string text);
        void SendEnter();
        void SendDelete();
        void Disconnect();
    }

    /// <summary>
    /// Raised when a transport's connection state changes or a connection
    /// attempt fails. MainForm subscribes to these to update its UI, the
    /// same role the Mac app's NSNotificationCenter notifications play.
    /// </summary>
    public sealed class TransportEventArgs : EventArgs
    {
        public string DeviceName { get; }
        public string Message { get; }

        public TransportEventArgs(string deviceName = null, string message = null)
        {
            DeviceName = deviceName;
            Message = message;
        }
    }
}
