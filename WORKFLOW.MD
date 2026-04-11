# 🛠 Project Workflow

Um die Stabilität des `main`-Branch zu gewährleisten und den Entwicklungsprozess strukturiert zu halten, folgen wir dem **GitHub Flow**. Bitte halte dich an die folgenden Schritte.

## 1. Ein Ticket wählen
* Schau in die **GitHub Issues / Unity-Multiplayer-Game-Backlog**, um zu sehen, welche Aufgaben oder Bugs gerade offen sind.
* Weise dir das Issue selbst zu oder schreibe einen kurzen Kommentar, dass du daran arbeitest.

## 2. Einen Branch erstellen
Pushe niemals direkt in den `main`-Branch. Erstelle stattdessen für jede Aufgabe einen eigenen Branch.
* **Naming Convention:**
    * `[Ticket-Nr]-feature-[Name des Tickets]` (für neue Funktionen)
    * `[Ticket-Nr]-bugfix-[Name des Tickets]` (für Fehlerbehebungen)
    * `[Ticket-Nr]-docs-[Name des Tickets]` (für Dokumentation)

```bash
# Branch erstellen:
git branch [Name]

# Branch wechseln
git checkout [Name]
```
> Jede neue Branch muss noch published werden!

## 3. Entwickeln & Committen
- Schreibe deinen Code lokal in Unity.

- Mache regelmäßige Commits mit aussagekräftigen Nachrichten.

- Achte darauf, dass das Projekt vor dem Pushen kompiliert und keine Fehler wirft.

## 4. Pull Request (PR) erstellen

Wenn deine Aufgabe fertig ist, eröffne auf GitHub einen Pull Request gegen den main-Branch.

- Verlinke das Issue in der Beschreibung (z. B. Closes #12).

- Beschreibe kurz, was du geändert hast.

# 5. Review & Merge

- Jemand wird den Code prüfen.

- Wenn alles passt, wird der PR in den main gemerget.