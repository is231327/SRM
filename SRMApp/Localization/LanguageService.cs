namespace SRMApp.Localization;

public class LanguageService
{
    private readonly Dictionary<string, (string En, string De)> _translations = new()
    {
        ["AppTitle"] = ("Server Room Monitoring", "Serverraum-Monitoring"),
        ["Home"] = ("Home", "Start"),
        ["Dashboard"] = ("Dashboard", "Dashboard"),
        ["Customers"] = ("Customers", "Kunden"),
        ["ServerRooms"] = ("Server Rooms", "Serverraeume"),
        ["Agents"] = ("Agents", "Agenten"),
        ["ShellyDevices"] = ("Shelly Devices", "Shelly-Geraete"),
        ["MonitoredDevices"] = ("Monitored Devices", "Ueberwachte Geraete"),
        ["MaintenanceWindows"] = ("Maintenance Windows", "Wartungsfenster"),
        ["SensorReadings"] = ("Sensor Readings", "Sensordaten"),
        ["Help"] = ("Help", "Hilfe"),
        ["Contact"] = ("Contact", "Kontakt"),
        ["Overview"] = ("Overview", "Uebersicht"),
        ["Create"] = ("Create", "Erstellen"),
        ["Update"] = ("Update", "Aktualisieren"),
        ["Delete"] = ("Delete", "Loeschen"),
        ["Save"] = ("Save", "Speichern"),
        ["Cancel"] = ("Cancel", "Abbrechen"),
        ["Language"] = ("Language", "Sprache"),
        ["English"] = ("English", "Englisch"),
        ["German"] = ("German", "Deutsch"),
        ["Loading"] = ("Loading...", "Laden..."),
        ["NoData"] = ("No data available.", "Keine Daten verfuegbar."),
        ["QuickActions"] = ("Quick Actions", "Schnellzugriff"),
        ["SystemStatus"] = ("System Status", "Systemstatus"),
        ["WelcomeHeadline"] = ("Monitor every room. Control every detail.", "Ueberwachen Sie jeden Raum. Steuern Sie jedes Detail."),
        ["WelcomeText"] = ("Use the dashboard and hierarchical management pages to configure customers, rooms, agents, Shelly devices, monitored devices, maintenance windows, and sensor readings.", "Verwenden Sie das Dashboard und die hierarchischen Verwaltungsseiten, um Kunden, Raeume, Agenten, Shelly-Geraete, ueberwachte Geraete, Wartungsfenster und Sensordaten zu konfigurieren."),
        ["HelpText"] = ("Use the customer page to start the hierarchy. Navigate deeper through server rooms, agents, and technical devices.", "Verwenden Sie die Kundenseite als Einstieg in die Hierarchie. Navigieren Sie dann tiefer zu Serverraeumen, Agenten und technischen Geraeten."),
        ["ContactText"] = ("For project support, coordinate through your internal team channels and issue tracker.", "Fuer Projektunterstuetzung koordinieren Sie sich ueber Ihre internen Teamkanaele und den Issue-Tracker."),
        ["CustomersCard"] = ("Customers", "Kunden"),
        ["RoomsCard"] = ("Rooms", "Raeume"),
        ["AgentsCard"] = ("Agents", "Agenten"),
        ["SensorsCard"] = ("Sensor Readings", "Sensordaten"),
        ["HierarchyHint"] = ("Manage child entities from the parent context for a clearer operational view.", "Verwalten Sie Kind-Entitaeten aus dem Elternkontext fuer eine klarere operative Sicht."),
        ["Name"] = ("Name", "Name"),
        ["Description"] = ("Description", "Beschreibung"),
        ["Actions"] = ("Actions", "Aktionen"),
        ["Active"] = ("Active", "Aktiv"),
        ["Edit"] = ("Edit", "Bearbeiten"),
        ["BackToCustomers"] = ("Back to Customers", "Zurueck zu Kunden"),
        ["BackToServerRooms"] = ("Back to Server Rooms", "Zurueck zu Serverraeumen"),
        ["BackToAgents"] = ("Back to Agents", "Zurueck zu Agenten"),
        ["CustomerChildren"] = ("Customer Hierarchy", "Kundenhierarchie"),
        ["ServerRoomChildren"] = ("Server Room Hierarchy", "Serverraum-Hierarchie"),
        ["AgentChildren"] = ("Agent Hierarchy", "Agenten-Hierarchie"),
        ["MaintenanceTitle"] = ("Title", "Titel"),
        ["RecordedAt"] = ("Recorded At", "Erfasst am")
    };

    public string CurrentLanguage { get; private set; } = "en";

    public event Action? LanguageChanged;

    public string T(string key)
    {
        if (!_translations.TryGetValue(key, out var entry))
        {
            return key;
        }

        return CurrentLanguage == "de" ? entry.De : entry.En;
    }

    public void SetLanguage(string language)
    {
        if (language != "en" && language != "de")
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }
}
