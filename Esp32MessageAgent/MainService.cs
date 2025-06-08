using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Esp32MessageAgent
{
    public partial class MainService : ServiceBase
    {
        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _workerTask;

        private bool _pinged = false;
        private CancellationTokenSource _shutdownCts;

        const int SHUTDOWN_DELAY_SECONDS = 30; // Delay before shutdown in seconds
        const string COM_PORT = "COM5";

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Log("Service starting...");

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _workerTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!SerialPort.GetPortNames().Contains(COM_PORT))
                        {
                            Log($"{COM_PORT} not found. Is the ESP32 connected? Retrying in 30s...");
                            _pinged = false;
                            await Task.Delay(30000, token); // wait 30 seconds before retry
                            continue;
                        }

                        if (_serialPort == null || !_serialPort.IsOpen)
                        {
                            _serialPort?.Dispose();

                            _serialPort = new SerialPort(COM_PORT, 115200)
                            {
                                NewLine = "\n",
                                DtrEnable = true,
                                RtsEnable = true,
                                ReadTimeout = 500,
                                WriteTimeout = 500
                            };

                            _serialPort.DataReceived += SerialPort_DataReceived;
                            _serialPort.Open();
                            _pinged = false;
                            Log($"Serial port {COM_PORT} opened.");
                        }

                        // Send message
                        string message = "on";
                        _serialPort.WriteLine(message);
                        if (_pinged == false)
                        {
                            Log($"Sent: {message}"); // log just once
                            _pinged = true;
                        }

                        await Task.Delay(5000, token); // send every 5 seconds
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested, exit cleanly
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log($"Error: {ex.Message}. Retrying in 30s...");
                        _pinged = false;
                        await Task.Delay(30000, token); // wait 30 seconds before retry on error
                    }
                }
            }, token);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var line = _serialPort.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Log($"Received from ESP32: {line}");

                    if (line.Equals("request_shutdown", StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Request shutdown command received from ESP32.");
                        _shutdownCts = new CancellationTokenSource();
                        Task.Run(() => RequestShutdownComputer(_shutdownCts.Token)); // Run async safely
                    }
                    else if (line.Equals("cancel_shutdown", StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Cancel shutdown command received from ESP32.");
                        _shutdownCts.Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Read error: {ex.Message}");
            }
        }

        private async Task RequestShutdownComputer(CancellationToken cancellationToken)
        {
            try
            {
                // delay for shutdown
                for (int i = 0; i < SHUTDOWN_DELAY_SECONDS; i++)
                {
                    Log($"Shutdown in {SHUTDOWN_DELAY_SECONDS - i}s.");
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000, cancellationToken);
                }

                var psi = new ProcessStartInfo("shutdown", "/s /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                Log("Shutdowned.");

            }
            catch (OperationCanceledException)
            {
                Log("Shutdown cancelled by user.");
            }
            catch (Exception ex)
            {
                Log($"Request shutdown failed: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            try
            {
                Log("Service stopping...");

                _cancellationTokenSource?.Cancel();
                _workerTask?.Wait();

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= SerialPort_DataReceived;
                        _serialPort.Close();
                        Log("Serial port closed.");
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }

                Log("Service stopped cleanly.");
            }
            catch (Exception ex)
            {
                Log($"OnStop error: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText("D:\\Wolfy Inc\\Publish\\Esp32Service\\Esp32ServiceLog.txt", logMessage);
            }
            catch
            {
                // Ignore logging failures (e.g. file locked)
            }
        }
    }
}
