# Agents.md

Dieses Dokument definiert die Agents für die Entwicklung des Rhino C# Plugins zur ERP-Artikelabfrage (BormBusiness via SQL). Es wird stets aktuell gehalten – bei Änderungen im Projekt (z. B. neue Features) wird es aktualisiert. Agents folgen einem kollaborativen Workflow: Architect plant, Coder implementiert, Tester validiert.

## Wichtige Regeln

Wenn ein Agent sich bei einer Rhino-spezifischen Implementierung (z. B. UI, Commands, UserData) nicht sicher ist, schaut er zuerst in den offiziellen Rhino Developer Docs nach: [developer.rhino3d.com](https://developer.rhino3d.com). Suche nach relevanten Guides (z. B. "RhinoCommon Panels" oder API-Referenzen wie Rhino.UI.Panels).

## Projekt-Überblick

### Ziel
Rhino-Plugin für Echtzeit-Suche und Übertragung von ERP-Artikeln (BormBusiness SQL-DB) auf Rhino-Objekte.

### Tech-Stack
- C# (.NET)
- RhinoCommon
- Eto.Forms (für UI)
- Dapper (für DB-Zugriff)

### Setup
- Visual Studio mit "RhinoCommon Plugin for Rhino 3D (C#)"-Template
- Boilerplate enthält:
  - Plugin-Klasse (erbt von Rhino.Plugins.Plugin)
  - Command-Klasse (erbt von Rhino.Commands.Command)
  - Debugging: F5 startet Rhino, installiere Plugin via Tools > Options > Plugins

### UI-Konzept
- Rhino-native dockable Panels (siehe Agent "UI Architect")
- Erweiterbarkeit: Basierend auf bestehendem CSV-Plugin – erweitere für DB-Quelle

### Verlinkte Dateien
- **Architecture.md**: High-Level-Architektur (Daten-, Business-, Präsentations-Schicht) *(Erstelle bei Bedarf)*
- **DBAccess.md**: Details zu SQL-Queries und Dapper-Integration *(Erstelle bei DB-Entwicklung)*
- **Testing.md**: Unit-Tests mit xUnit/Moq *(Erstelle bei Testing-Phase)*

## Agent-Definitionen

Jeder Agent hat eine Rolle, Verantwortlichkeiten und Richtlinien. Verwende sie in Cursor für gezielte Code-Generierung.

### 1. Architect Agent

**Rolle:** Plant die Gesamtstruktur und erweitert den Boilerplate.

**Verantwortlichkeiten:**
- Definiere Klassen-Struktur: z. B. ArtikelRepository für DB, DataMapper für UserData-Übertragung
- Integriere Rhino-native Panel: Erstelle dockables Panel als Einstiegspunkt
- Stelle Skalierbarkeit sicher (z. B. Async für Echtzeit-Queries)

**Richtlinien:**
- Starte einfach: Erweitere Boilerplate-Command, um Panel zu öffnen
- Bei Unsicherheit: Rhino Docs nachschlagen (z. B. Your First Plugin Guide)
- Beispiel für Panel-Integration: Siehe unten unter "UI Architect"



### 2. UI Architect Agent

**Rolle:** Handhabt Rhino-spezifische UI, insbesondere dockable Panels.

**Verantwortlichkeiten:**
- Erstelle und registriere ein dockables Panel (z. B. für Artikel-Suche und Liste)
- UI-Elemente: TextBox für Suche, DataGridView für Ergebnisse, Button für Übertragung auf Objekte

**Richtlinien:**
- Verwende Eto.Forms für Cross-Platform
- Bei Unsicherheit: Schau in Panels API-Doc oder Panels Guide

**Code-Beispiel für Dockable Panel (basierend auf Rhino Docs):**

```csharp
using Eto.Forms;
using Rhino.UI;

public class ErpSearchPanel : Panel  // Erbe von Panel (für dockable)
{
    public ErpSearchPanel()
    {
        // Initialisiere UI
        var layout = new DynamicLayout();
        layout.Add(new Label { Text = "ERP Artikel Suche" });
        var searchBox = new TextBox { PlaceholderText = "Suche nach Artikel..." };
        layout.Add(searchBox);
        // Füge Liste, Buttons hinzu...
        Content = layout;
    }
}

// In der Plugin-Klasse (z. B. in OnLoad):
public class MyPlugin : Rhino.Plugins.Plugin
{
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        Panels.RegisterPanel(this, typeof(ErpSearchPanel), "ERP Suche", null);  // Registriere als dockable
        return LoadReturnCode.Success;
    }
}
```

**Integration als Dockbar:** Nach Registrierung erscheint es unter View > Panels in Rhino. Setze PanelStyle = PanelStyles.Dockable für Docking-Optionen.



### 3. Coder Agent

**Rolle:** Implementiert Logik basierend auf Architect-Plänen.

**Verantwortlichkeiten:**
- Schreibe DB-Zugriff (Dapper-Queries, async)
- Erweitere CSV-Plugin für DB-Übertragung zu UserData
- Implementiere Commands (z. B. ErpSuche öffnet Panel)

**Richtlinien:**
- Verwende async/await für nicht-blockierende DB-Calls
- Beispiel-Query: `SELECT * FROM Artikel WHERE Bezeichnung LIKE @term`
- Bei Rhino-spezifisch: Docs nachschlagen (z. B. UserData: UserData Guide)



### 4. Tester Agent

**Rolle:** Stellt Qualität und Skalierbarkeit sicher.

**Verantwortlichkeiten:**
- Schreibe Unit-Tests (xUnit, Moq für Mocks)
- Teste Integration in Rhino (z. B. Panel-Docking, DB-Queries)

**Richtlinien:**
- Coverage: 80%+
- Beispiel: Mock DB-Connection für Repository-Tests
- Verlinkung: Details in Testing.md



## Workflow in Cursor

1. **Projekt öffnen**: Öffne Projekt in Cursor
2. **Agent-Nutzung**: Verwende Agents: z. B. "@Architect: Plane Panel-Integration"
3. **Dokumentation**: Aktualisiere Agents.md bei Fortschritt (z. B. neue Agents hinzufügen)
4. **Version Control**: Commit-Änderungen: Halte Repo sauber

**Letzte Aktualisierung:** 27. Oktober 2025
**Hinweis:** Erweitere bei Bedarf (z. B. nach DB-Implementierung)