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
        ["MonitoredDevicePingResults"] = ("Monitored Device Ping Results", "Ping-Ergebnisse ueberwachter Geraete"),
        ["MaintenanceWindows"] = ("Maintenance Windows", "Wartungsfenster"),
        ["SensorReadings"] = ("Sensor Readings", "Sensordaten"),
        ["Users"] = ("Users", "Benutzer"),
        ["AgentCredentials"] = ("Agent Credentials", "Agent-Zugangsdaten"),
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
        ["Login"] = ("Login", "Anmelden"),
        ["Logout"] = ("Logout", "Abmelden"),
        ["Inactive"] = ("Inactive", "Inaktiv"),
        ["BackToCustomers"] = ("Back to Customers", "Zurueck zu Kunden"),
        ["BackToServerRooms"] = ("Back to Server Rooms", "Zurueck zu Serverraeumen"),
        ["BackToAgents"] = ("Back to Agents", "Zurueck zu Agenten"),
        ["CustomerChildren"] = ("Customer Hierarchy", "Kundenhierarchie"),
        ["ServerRoomChildren"] = ("Server Room Hierarchy", "Serverraum-Hierarchie"),
        ["AgentChildren"] = ("Agent Hierarchy", "Agenten-Hierarchie"),
        ["MaintenanceTitle"] = ("Title", "Titel"),
        ["RecordedAt"] = ("Recorded At", "Erfasst am"),
        ["Reachable"] = ("Reachable", "Erreichbar"),
        ["Roundtrip"] = ("Roundtrip", "Laufzeit"),
        ["FailureCount"] = ("Failure Count", "Fehleranzahl"),
        ["ThresholdReached"] = ("Threshold Reached", "Schwellwert erreicht"),
        ["ErrorMessage"] = ("Error Message", "Fehlermeldung"),
        ["Username"] = ("Username", "Benutzername"),
        ["Email"] = ("Email", "E-Mail"),
        ["FirstName"] = ("First Name", "Vorname"),
        ["LastName"] = ("Last Name", "Nachname"),
        ["Phone"] = ("Phone", "Telefon"),
        ["Password"] = ("Password", "Passwort"),
        ["CurrentPassword"] = ("Current Password", "Aktuelles Passwort"),
        ["NewPassword"] = ("New Password", "Neues Passwort"),
        ["Role"] = ("Role", "Rolle"),
        ["Customer"] = ("Customer", "Kunde"),
        ["NoCustomer"] = ("No customer", "Kein Kunde"),
        ["UnknownCustomer"] = ("Unknown customer", "Unbekannter Kunde"),
        ["OwnCustomer"] = ("Own customer", "Eigener Kunde"),
        ["Profile"] = ("Profile", "Profil"),
        ["ProfileDescription"] = ("Manage your own contact data and password.", "Verwalten Sie Ihre eigenen Kontaktdaten und Ihr Passwort."),
        ["ProfileDetails"] = ("Profile Details", "Profildaten"),
        ["SaveProfile"] = ("Save Profile", "Profil speichern"),
        ["ProfileSaved"] = ("Your profile was updated.", "Ihr Profil wurde aktualisiert."),
        ["ProfileSaveFailed"] = ("Your profile could not be updated.", "Ihr Profil konnte nicht aktualisiert werden."),
        ["ProfileLoadFailed"] = ("Your profile could not be loaded.", "Ihr Profil konnte nicht geladen werden."),
        ["ChangePassword"] = ("Change Password", "Passwort aendern"),
        ["PasswordChanged"] = ("Your password was changed.", "Ihr Passwort wurde geaendert."),
        ["PasswordChangeFailed"] = ("Your password could not be changed.", "Ihr Passwort konnte nicht geaendert werden."),
        ["Never"] = ("Never", "Nie"),
        ["LastLogin"] = ("Last Login", "Letzte Anmeldung"),
        ["UserRoles"] = ("Roles", "Rollen"),
        ["UsersLoginRequired"] = ("You need to log in first.", "Sie muessen sich zuerst anmelden."),
        ["UsersAdminOnly"] = ("Only authorized user managers may manage users.", "Nur berechtigte Benutzerverwalter duerfen Benutzer verwalten."),
        ["UsersPageDescription"] = ("Create, review, and update platform or customer users.", "Erstellen, pruefen und aktualisieren Sie Plattform- oder Kundenbenutzer."),
        ["AgentCredentialsPageDescription"] = ("Create, review, and update machine credentials for deployed agents.", "Erstellen, pruefen und aktualisieren Sie Maschinenzugangsdaten fuer bereitgestellte Agenten."),
        ["UserCreated"] = ("User '{0}' was created.", "Benutzer '{0}' wurde erstellt."),
        ["UserUpdated"] = ("User '{0}' was updated.", "Benutzer '{0}' wurde aktualisiert."),
        ["AgentCredentialCreated"] = ("Agent credential '{0}' was created.", "Agent-Zugangsdaten '{0}' wurden erstellt."),
        ["AgentCredentialUpdated"] = ("Agent credential '{0}' was updated.", "Agent-Zugangsdaten '{0}' wurden aktualisiert."),
        ["PasswordRequired"] = ("A password is required when creating a user.", "Beim Erstellen eines Benutzers ist ein Passwort erforderlich."),
        ["ClientIdentifier"] = ("Client Identifier", "Client-Identifier"),
        ["ClientSecret"] = ("Client Secret", "Client-Secret"),
        ["AgentReference"] = ("Agent", "Agent"),
        ["RotateSecretOptional"] = ("Leave empty to keep the current secret.", "Leer lassen, um das aktuelle Secret beizubehalten."),
        ["AdminPasswordReset"] = ("Administrative Password Reset", "Administratives Passwort-Reset"),
        ["ResetPassword"] = ("Reset Password", "Passwort zuruecksetzen"),
        ["ManagedPasswordResetSuccess"] = ("The managed user's password was reset.", "Das Passwort des verwalteten Benutzers wurde zurueckgesetzt."),
        ["PasswordPolicyHint"] = ("Password policy: at least 12 characters, including uppercase, lowercase, digit, and special character.", "Passwortrichtlinie: mindestens 12 Zeichen mit Grossbuchstaben, Kleinbuchstaben, Ziffer und Sonderzeichen."),
        ["MustChangePasswordNotice"] = ("Your password was reset by an administrator. You must set a new password before continuing.", "Ihr Passwort wurde von einem Administrator zurueckgesetzt. Sie muessen zuerst ein neues Passwort setzen.")
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
