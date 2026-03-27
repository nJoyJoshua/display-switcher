# DisplaySwitcher 🖥

A lightweight Windows system tray tool for switching between monitor profiles and audio devices with a single click.

**Ein System-Tray-Tool zum schnellen Wechseln zwischen Monitor-Profilen + Audio-Ausgabe.**

## Features

- ✅ System Tray Icon (unten rechts in der Taskleiste)
- ✅ Rechtsklick → Profil direkt wechseln
- ✅ Beliebig viele Profile konfigurierbar
- ✅ Monitor ein-/ausschalten per Windows `SetDisplayConfig` API
- ✅ Audio-Ausgabe- und Eingabegerät automatisch mitschalten
- ✅ Auflösung, Position & Bildwiederholrate pro Profil gespeichert
- ✅ Autostart mit Windows (optional)
- ✅ Einzel-`.exe`, kein Install nötig
- ✅ Dunkles Kontextmenü-Design

---

## Voraussetzungen

- Windows 10 / 11 (64-bit)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (nur zum Kompilieren; die fertige `.exe` ist self-contained)

---

## Kompilieren & Starten

### Option A – Batch-Skript (empfohlen)
```
build.bat
```
Die fertige `DisplaySwitcher.exe` liegt danach im Ordner `Build\`.

### Option B – Manuell
```cmd
cd DisplaySwitcher
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ..\Build
```

---

## Erste Schritte

1. `DisplaySwitcher.exe` starten → Icon erscheint in der Taskleiste
2. **Rechtsklick** auf das Icon → „Profile konfigurieren" → „Neues Profil erstellen"
3. Profil benennen (z. B. „Arbeitsplatz 3 Screens" oder „Heimkino")
4. Monitore auswählen, die in diesem Profil **aktiv** sein sollen
5. Optional: Audio-Ausgabe- und Eingabegerät für dieses Profil wählen
6. Speichern → wiederhole für weitere Profile

### Meine Monitore identifizieren
- Windows-Taste → Einstellungen → System → Anzeige → **Identifizieren**
- Windows zeigt kurz die Nummer auf jedem Monitor an
- Im Tool siehst du den Gerätenamen + `[\\.\DISPLAY1]` usw.

---

## Tipp: Als Administrator ausführen

Falls Monitore nicht erkannt werden oder das Umschalten fehlschlägt:
→ Rechtsklick auf `DisplaySwitcher.exe` → „Als Administrator ausführen"

Oder dauerhaft: Rechtsklick → Eigenschaften → Kompatibilität → „Als Administrator ausführen"

---

## Einstellungen

Die Konfiguration wird gespeichert unter:
```
%AppData%\DisplaySwitcher\settings.json
```
(Im Tray-Menü: „Einstellungsordner öffnen")

Ein Debug-Log wird ebenfalls dort gespeichert: `display_log.txt`

---

## Problemlösung

| Problem | Lösung |
|---|---|
| Monitore werden nicht erkannt | Als Administrator ausführen |
| Monitor schaltet sich nicht um | Treiber aktuell? Anderer Port? DisplayPort/HDMI aktiv? |
| Audio ändert sich nicht | Gerät in Windows-Sounds als aktiv markiert? |
| Icon erscheint nicht | Taskleiste → Ausgeblendete Symbole anzeigen (Pfeil ∧) |
| App startet nicht doppelt | Single-Instance-Schutz ist eingebaut |

---

## Technologie

- C# / .NET 8 / Windows Forms
- Windows `QueryDisplayConfig` / `SetDisplayConfig` API (P/Invoke)
- Windows Core Audio `IPolicyConfig` COM Interface
- Self-contained Single-File Publish (keine .NET-Installation auf dem Zielrechner nötig)

---

## Lizenz

Dieses Projekt steht unter der [MIT License](LICENSE).
