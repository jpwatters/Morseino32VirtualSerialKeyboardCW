using System;
using System.IO.Ports;

namespace MorserinoWinKeyboard
{
    /// <summary>
    /// Speaks the Morserino-32 "M32 Protocol" v1.3 over USB serial: plain
    /// text written to the port is NOT interpreted as keystrokes -- the
    /// device expects line-based "PUT"/"GET" commands. Sent text is played
    /// as Morse via `PUT cw/play/&lt;text&gt;`, which requires the Morserino to
    /// already be in CW Keyer or Morse Trx mode. See:
    /// https://github.com/oe1wkl/Morserino-32/blob/master/Documentation/Protocol%20Description/M32%20Protocol.md
    ///
    /// This is a line-for-line port of the Mac app's SerialKeyboard.m: same
    /// 115200 baud / 8N1 settings, same three commands, same single "\n"
    /// line terminator (not "\r\n" -- the M32 protocol only wants a bare
    /// line feed).
    /// </summary>
    public sealed class SerialKeyboard : IKeyboardTransport, IDisposable
    {
        private SerialPort _port;

        public event EventHandler<TransportEventArgs> Connected;
        public event EventHandler Disconnected;
        public event EventHandler<TransportEventArgs> Failed;

        public bool IsConnected => _port != null && _port.IsOpen;
        public string ConnectedDeviceName { get; private set; }

        /// <summary>
        /// Lists every COM port currently present, the Windows equivalent of
        /// the Mac app's +availableSerialPortPaths (which scans /dev for
        /// cu.* entries).
        /// </summary>
        public static string[] AvailablePortNames()
        {
            string[] names = SerialPort.GetPortNames();
            Array.Sort(names, StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>
        /// Opens the given COM port and switches the Morserino into M32
        /// protocol mode. Returns false (and raises Failed) if the port
        /// couldn't be opened, the same contract as the Mac app's
        /// -connectToPath:error:.
        /// </summary>
        public bool Connect(string portName, out string errorMessage)
        {
            Disconnect();
            errorMessage = null;

            try
            {
                _port = new SerialPort(portName)
                {
                    BaudRate = 115200,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    NewLine = "\n",
                    // Small, infrequent writes -- generous timeouts are fine
                    // and avoid spurious failures on slower USB-serial
                    // adapters.
                    WriteTimeout = 2000,
                    ReadTimeout = 2000,
                };
                _port.Open();
            }
            catch (Exception ex)
            {
                errorMessage = $"Couldn't open {portName}: {ex.Message}";
                _port = null;
                Failed?.Invoke(this, new TransportEventArgs(message: errorMessage));
                return false;
            }

            ConnectedDeviceName = portName;

            // Turn on the M32 protocol. This also suspends the Morserino's
            // sleep timeout for as long as we're connected -- without it,
            // the device can fall asleep mid-session and drop the port.
            WriteCommand("PUT device/protocol/on");

            Connected?.Invoke(this, new TransportEventArgs(deviceName: ConnectedDeviceName));
            return true;
        }

        public void SendString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            WriteCommand("PUT cw/play/" + text);
        }

        public void SendEnter()
        {
            // No literal "Enter" concept in the M32 protocol; repurposed as
            // "stop playing" (same effect as keying manually mid-playback).
            WriteCommand("PUT cw/stop");
        }

        public void SendDelete()
        {
            WriteCommand("PUT cw/stop");
        }

        public void Disconnect()
        {
            if (_port == null)
            {
                return;
            }

            bool wasConnected = IsConnected;
            try
            {
                if (_port.IsOpen)
                {
                    // Let the device know we're done so its normal sleep
                    // timeout resumes.
                    WriteCommand("PUT device/protocol/off");
                    _port.Close();
                }
            }
            catch
            {
                // Most likely the device was already unplugged; nothing
                // more to do here.
            }
            finally
            {
                _port.Dispose();
                _port = null;
                ConnectedDeviceName = null;
            }

            if (wasConnected)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private void WriteCommand(string command)
        {
            if (_port == null || !_port.IsOpen)
            {
                return;
            }

            try
            {
                _port.WriteLine(command);
            }
            catch
            {
                // Most likely the device was unplugged mid-write.
                Disconnect();
            }
        }

        public void Dispose() => Disconnect();
    }
}
