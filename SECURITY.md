# Security & Privacy Policy

## Privacy Guarantee

Prononce is built with a strict **offline-first, zero-knowledge** architecture:

- **Zero Cloud Requests**: All speech synthesis uses Windows built-in voices (`Windows.Media.SpeechSynthesis`).
- **Zero Telemetry or Tracking**: The application does not include any analytics, crash reporting, or user tracking libraries.
- **Zero Data Persistence of Selections**: Selected text is processed in memory for speech and immediately discarded. No text history is logged or saved to disk.
- **Clipboard Safety**: When synthetic copy is used as a fallback, all original clipboard formats and contents are restored in memory immediately.

## Reporting a Vulnerability

If you discover a security vulnerability in Prononce, please report it responsibly by opening a private security advisory on GitHub or emailing the maintainer.
