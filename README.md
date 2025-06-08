# Esp32MessageAgent

A Windows Service that communicates with an ESP32 via a serial (COM) port to handle remote shutdown requests with countdown and cancellation support. Designed for scenarios where the ESP32 is used as a physical controller or sensor hub to manage a computer remotely.

---

## 🔒 Disclaimer

This service will **force shutdown** the machine if the command is not canceled in time. Use with care. Do not deploy in production or server environments unless safeguards are in place.
