namespace SRMApp.Localization;

public class LanguageService
{
    private readonly Dictionary<string, (string En, string De)> _translations = new()
    {
        ["AppTitle"] = ("Server Room Monitoring", "Serverraum-Monitoring"),
        ["Home"] = ("Home", "Start"),
        ["Dashboard"] = ("Dashboard", "Dashboard"),
        ["Operations"] = ("Operations", "Betrieb"),
        ["Support"] = ("Support", "Support"),
        ["Customers"] = ("Customers", "Kunden"),
        ["ServerRooms"] = ("Server Rooms", "Serverraeume"),
        ["Agents"] = ("Agents", "Agenten"),
        ["Version"] = ("Version", "Version"),
        ["ShellyDevices"] = ("Shelly Devices", "Shelly-Geraete"),
        ["MonitoredDevices"] = ("Monitored Devices", "Ueberwachte Geraete"),
        ["MonitoredDevicePingResults"] = ("Monitored Device Ping Results", "Ping-Ergebnisse ueberwachter Geraete"),
        ["MaintenanceWindows"] = ("Maintenance Windows", "Wartungsfenster"),
        ["SensorReadings"] = ("Sensor Readings", "Sensordaten"),
        ["Incidents"] = ("Incidents", "Vorfalle"),
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
        ["NoMatchingData"] = ("No entries match the selected filters.", "Keine Eintraege entsprechen den ausgewaehlten Filtern."),
        ["FilterAndSorting"] = ("Filter and sorting", "Filter und Sortierung"),
        ["Search"] = ("Search", "Suchen"),
        ["SearchIncidentsPlaceholder"] = ("Ticket number, incident, room, or device", "Ticketnummer, Vorfall, Raum oder Geraet"),
        ["SearchUsersPlaceholder"] = ("Username, name, or email", "Benutzername, Name oder E-Mail"),
        ["All"] = ("All", "Alle"),
        ["IncidentType"] = ("Incident Type", "Vorfalltyp"),
        ["SortBy"] = ("Sort by", "Sortieren nach"),
        ["ResetFilters"] = ("Reset filters", "Filter zuruecksetzen"),
        ["ShowingResults"] = ("Showing {0} of {1} entries.", "{0} von {1} Eintraegen werden angezeigt."),
        ["DateNewest"] = ("Date: newest first", "Datum: neueste zuerst"),
        ["DateOldest"] = ("Date: oldest first", "Datum: aelteste zuerst"),
        ["PriorityHighest"] = ("Priority: highest first", "Prioritaet: hoechste zuerst"),
        ["PriorityLowest"] = ("Priority: lowest first", "Prioritaet: niedrigste zuerst"),
        ["ServerRoomAscending"] = ("Server room: A-Z", "Serverraum: A-Z"),
        ["ServerRoomDescending"] = ("Server room: Z-A", "Serverraum: Z-A"),
        ["UsernameAscending"] = ("Username: A-Z", "Benutzername: A-Z"),
        ["UsernameDescending"] = ("Username: Z-A", "Benutzername: Z-A"),
        ["NameAscending"] = ("Name: A-Z", "Name: A-Z"),
        ["NameDescending"] = ("Name: Z-A", "Name: Z-A"),
        ["LastLoginNewest"] = ("Last login: newest first", "Letzte Anmeldung: neueste zuerst"),
        ["LastLoginOldest"] = ("Last login: oldest first", "Letzte Anmeldung: aelteste zuerst"),
        ["Reachability"] = ("Reachability", "Erreichbarkeit"),
        ["RoundtripFastest"] = ("Roundtrip: fastest first", "Laufzeit: schnellste zuerst"),
        ["RoundtripSlowest"] = ("Roundtrip: slowest first", "Laufzeit: langsamste zuerst"),
        ["FailureCountHighest"] = ("Failure count: highest first", "Fehleranzahl: hoechste zuerst"),
        ["FailureCountLowest"] = ("Failure count: lowest first", "Fehleranzahl: niedrigste zuerst"),
        ["TemperatureHighest"] = ("Temperature: highest first", "Temperatur: hoechste zuerst"),
        ["TemperatureLowest"] = ("Temperature: lowest first", "Temperatur: niedrigste zuerst"),
        ["BatteryHighest"] = ("Battery: highest first", "Batterie: hoechste zuerst"),
        ["BatteryLowest"] = ("Battery: lowest first", "Batterie: niedrigste zuerst"),
        ["BrightnessHighest"] = ("Brightness: highest first", "Helligkeit: hoechste zuerst"),
        ["BrightnessLowest"] = ("Brightness: lowest first", "Helligkeit: niedrigste zuerst"),
        ["SystemStatus"] = ("System Status", "Systemstatus"),
        ["MonitoringInventory"] = ("Monitoring Inventory", "Monitoring-Bestand"),
        ["WelcomeHeadline"] = ("Monitor every room. See what matters.", "Ueberwachen Sie jeden Raum. Sehen Sie, was wichtig ist."),
        ["WelcomeText"] = ("Keep an eye on server rooms, environmental conditions, connected equipment, and current incidents from one place.", "Behalten Sie Serverraeume, Umgebungsbedingungen, angeschlossene Geraete und aktuelle Vorfaelle an einem Ort im Blick."),
        ["HelpText"] = ("A simple guide to monitoring your server rooms and responding when something needs attention.", "Eine einfache Anleitung zur Ueberwachung Ihrer Serverraeume und zum Umgang mit wichtigen Meldungen."),
        ["ContactText"] = ("Contact our team if you need help with the server-room monitoring service.", "Kontaktieren Sie unser Team, wenn Sie Hilfe mit dem Serverraum-Monitoring benoetigen."),
        ["HelpGettingStartedTitle"] = ("Finding your way around", "Orientierung auf der Seite"),
        ["HelpGettingStartedText"] = ("Start on the dashboard for the overall situation. Open a customer and then a server room when you need a more detailed view.", "Beginnen Sie im Dashboard fuer die Gesamtsituation. Oeffnen Sie einen Kunden und danach einen Serverraum, wenn Sie mehr Details benoetigen."),
        ["HelpDashboardTitle"] = ("Understanding the dashboard", "Das Dashboard verstehen"),
        ["HelpDashboardText"] = ("The numbers show all configured records. Colors and supporting text highlight open incidents, inactive monitoring, or missing reports.", "Die Zahlen zeigen alle konfigurierten Eintraege. Farben und Begleittexte weisen auf offene Vorfaelle, inaktives Monitoring oder fehlende Meldungen hin."),
        ["HelpIncidentsTitle"] = ("Responding to incidents", "Auf Vorfaelle reagieren"),
        ["HelpIncidentsText"] = ("Open incidents describe conditions that still require attention. Use the Redmine ticket button to open the related support ticket.", "Offene Vorfaelle beschreiben Situationen, die weiterhin Aufmerksamkeit erfordern. Mit der Schaltflaeche Redmine-Ticket oeffnen Sie das zugehoerige Support-Ticket."),
        ["HelpMaintenanceTitle"] = ("Maintenance windows", "Wartungsfenster"),
        ["HelpMaintenanceText"] = ("A maintenance window marks an expected period of work. Opening a door during that period does not create a door incident.", "Ein Wartungsfenster kennzeichnet einen geplanten Arbeitszeitraum. Wird die Tuer in diesem Zeitraum geoeffnet, entsteht kein Tuervorfall."),
        ["FrequentlyAskedQuestions"] = ("Frequently asked questions", "Haeufig gestellte Fragen"),
        ["FaqDashboardQuestion"] = ("What do the tile colors mean?", "Was bedeuten die Farben der Kacheln?"),
        ["FaqDashboardAnswer"] = ("Green indicates a normal or resolved state, yellow indicates attention may be needed, red indicates an open critical condition, and blue indicates that information has not arrived yet.", "Gruen steht fuer einen normalen oder geloesten Zustand, Gelb fuer moeglichen Handlungsbedarf, Rot fuer einen offenen kritischen Zustand und Blau fuer noch nicht eingetroffene Informationen."),
        ["FaqIncidentQuestion"] = ("When is an incident resolved?", "Wann ist ein Vorfall geloest?"),
        ["FaqIncidentAnswer"] = ("The system resolves an incident after the reported condition returns to normal, for example when a door closes or a device becomes reachable again.", "Das System loest einen Vorfall, sobald sich der gemeldete Zustand normalisiert, zum Beispiel wenn eine Tuer geschlossen oder ein Geraet wieder erreichbar ist."),
        ["FaqTicketQuestion"] = ("Why can a Redmine ticket still be open after an incident is resolved?", "Warum kann ein Redmine-Ticket nach der Loesung eines Vorfalls noch offen sein?"),
        ["FaqTicketAnswer"] = ("SRM adds a resolution note to Redmine but does not close the external ticket automatically. The responsible person completes the ticket in Redmine.", "SRM fuegt in Redmine einen Loesungshinweis hinzu, schliesst das externe Ticket aber nicht automatisch. Die verantwortliche Person schliesst das Ticket in Redmine ab."),
        ["FaqMissingDataQuestion"] = ("Why does a tile say that no data has been received?", "Warum zeigt eine Kachel an, dass keine Daten empfangen wurden?"),
        ["FaqMissingDataAnswer"] = ("The appliance or device may not have reported yet. If the message remains, contact support and provide the affected customer, server room, and device.", "Die Appliance oder das Geraet hat moeglicherweise noch keine Daten gemeldet. Bleibt die Meldung bestehen, kontaktieren Sie den Support und nennen Sie Kunde, Serverraum und Geraet."),
        ["FaqConfigurationQuestion"] = ("Why can I view data but not change it?", "Warum kann ich Daten sehen, aber nicht aendern?"),
        ["FaqConfigurationAnswer"] = ("Customer accounts are read-only. Configuration changes are available only to authorized employees and administrators.", "Kundenkonten haben nur Leserechte. Konfigurationsaenderungen stehen nur berechtigten Mitarbeitern und Administratoren zur Verfuegung."),
        ["ContactCompanyTitle"] = ("Company and contact", "Unternehmen und Kontakt"),
        ["ContactChannelsTitle"] = ("Contact details", "Kontaktdaten"),
        ["Company"] = ("Company", "Unternehmen"),
        ["ContactPerson"] = ("Contact person", "Ansprechperson"),
        ["Address"] = ("Address", "Adresse"),
        ["SupportHours"] = ("Support hours", "Supportzeiten"),
        ["ContactSupportHoursValue"] = ("Monday to Friday, 08:00–17:00", "Montag bis Freitag, 08:00–17:00 Uhr"),
        ["Germany"] = ("Germany", "Deutschland"),
        ["CustomersCard"] = ("Customers", "Kunden"),
        ["RoomsCard"] = ("Rooms", "Raeume"),
        ["AgentsCard"] = ("Agents", "Agenten"),
        ["SensorsCard"] = ("Sensor Readings", "Sensordaten"),
        ["Name"] = ("Name", "Name"),
        ["Description"] = ("Description", "Beschreibung"),
        ["Actions"] = ("Actions", "Aktionen"),
        ["Active"] = ("Active", "Aktiv"),
        ["Status"] = ("Status", "Status"),
        ["Edit"] = ("Edit", "Bearbeiten"),
        ["Login"] = ("Login", "Anmelden"),
        ["SigningIn"] = ("Signing in...", "Anmeldung..."),
        ["LoginFailed"] = ("Login failed.", "Anmeldung fehlgeschlagen."),
        ["Mfa"] = ("MFA", "MFA"),
        ["MfaVerification"] = ("Multi-factor authentication", "Mehrfaktor-Authentifizierung"),
        ["MfaEnrollmentInstructions"] = ("Scan this QR code in Microsoft Authenticator, then enter the current six-digit code.", "Scannen Sie diesen QR-Code mit Microsoft Authenticator und geben Sie danach den aktuellen sechsstelligen Code ein."),
        ["MfaLoginInstructions"] = ("Enter the current code from Microsoft Authenticator.", "Geben Sie den aktuellen Code aus Microsoft Authenticator ein."),
        ["MfaQrCode"] = ("Microsoft Authenticator setup QR code", "QR-Code zur Einrichtung von Microsoft Authenticator"),
        ["MfaManualEntryKey"] = ("Manual setup key", "Manueller Einrichtungsschluessel"),
        ["VerificationCode"] = ("Verification code", "Bestaetigungscode"),
        ["Verify"] = ("Verify", "Bestaetigen"),
        ["Back"] = ("Back", "Zurueck"),
        ["RecoveryCodeAlternative"] = ("You can also enter one unused recovery code.", "Sie koennen auch einen noch nicht verwendeten Wiederherstellungscode eingeben."),
        ["RecoveryCodesWarning"] = ("Save these one-time recovery codes now. They will not be shown again.", "Speichern Sie diese einmal verwendbaren Wiederherstellungscodes jetzt. Sie werden nicht erneut angezeigt."),
        ["RecoveryCodesSaved"] = ("I saved the recovery codes", "Ich habe die Wiederherstellungscodes gespeichert"),
        ["MfaVerificationFailed"] = ("The verification code is invalid or expired.", "Der Bestaetigungscode ist ungueltig oder abgelaufen."),
        ["Enabled"] = ("Enabled", "Aktiviert"),
        ["EnrollmentRequired"] = ("Enrollment required", "Einrichtung erforderlich"),
        ["ResetMfa"] = ("Reset MFA", "MFA zuruecksetzen"),
        ["ResetMfaHelp"] = ("This revokes all sessions and requires a new Microsoft Authenticator enrollment at the next login.", "Dadurch werden alle Sitzungen widerrufen und bei der naechsten Anmeldung ist eine neue Einrichtung von Microsoft Authenticator erforderlich."),
        ["ManagedMfaResetSuccess"] = ("MFA was reset. The user must enroll again at the next login.", "MFA wurde zurueckgesetzt. Der Benutzer muss sie bei der naechsten Anmeldung erneut einrichten."),
        ["Logout"] = ("Logout", "Abmelden"),
        ["Inactive"] = ("Inactive", "Inaktiv"),
        ["BackToCustomers"] = ("Back to Customers", "Zurueck zu Kunden"),
        ["BackToServerRooms"] = ("Back to Server Rooms", "Zurueck zu Serverraeumen"),
        ["BackToAgents"] = ("Back to Agents", "Zurueck zu Agenten"),
        ["CustomerChildren"] = ("Customer Hierarchy", "Kundenhierarchie"),
        ["ServerRoomChildren"] = ("Server Room Hierarchy", "Serverraum-Hierarchie"),
        ["CustomerOverview"] = ("Customer Overview", "Kundenuebersicht"),
        ["CustomerOverviewDescription"] = ("Review the complete operational structure of one customer from a single page.", "Pruefen Sie die komplette operative Struktur eines Kunden auf einer Seite."),
        ["BackToCustomerOverview"] = ("Back to Customer Overview", "Zurueck zur Kundenuebersicht"),
        ["CustomersPageDescription"] = ("Create customers and open their operational dashboards.", "Erstellen Sie Kunden und oeffnen Sie deren operative Uebersichten."),
        ["CustomersOverviewDescription"] = ("Review all customers in one place and open their operational dashboards from dedicated cards.", "Pruefen Sie alle Kunden an einem Ort und oeffnen Sie deren operative Dashboards ueber eigene Karten."),
        ["ServerRoomsOverviewDescription"] = ("Review server rooms in card format and open dedicated create or edit forms.", "Pruefen Sie Serverraeume in Kartenform und oeffnen Sie eigene Erstellen- oder Bearbeiten-Formulare."),
        ["AgentsOverviewDescription"] = ("Review deployed agents in card format and open dedicated create or edit forms.", "Pruefen Sie eingesetzte Agenten in Kartenform und oeffnen Sie eigene Erstellen- oder Bearbeiten-Formulare."),
        ["ShellyDevicesOverviewDescription"] = ("Review configured Shelly devices in card format and open dedicated create or edit forms.", "Pruefen Sie konfigurierte Shelly-Geraete in Kartenform und oeffnen Sie eigene Erstellen- oder Bearbeiten-Formulare."),
        ["MonitoredDevicesOverviewDescription"] = ("Review monitored devices in card format and open dedicated create or edit forms.", "Pruefen Sie ueberwachte Geraete in Kartenform und oeffnen Sie eigene Erstellen- oder Bearbeiten-Formulare."),
        ["MaintenanceWindowsOverviewDescription"] = ("Review maintenance windows in card format and open dedicated create or edit forms.", "Pruefen Sie Wartungsfenster in Kartenform und oeffnen Sie eigene Erstellen- oder Bearbeiten-Formulare."),
        ["CustomersOverviewSectionDescription"] = ("Detailed customer cards with direct navigation to overview, edit, and delete actions.", "Detaillierte Kundenkarten mit direkter Navigation zu Uebersicht, Bearbeiten und Loeschen."),
        ["CreateCustomer"] = ("Create Customer", "Kunde erstellen"),
        ["CreateCustomerDescription"] = ("Create a new customer in a dedicated form.", "Erstellen Sie einen neuen Kunden in einem eigenen Formular."),
        ["CreateServerRoom"] = ("Create Server Room", "Serverraum erstellen"),
        ["CreateServerRoomDescription"] = ("Create a new server room in a dedicated form.", "Erstellen Sie einen neuen Serverraum in einem eigenen Formular."),
        ["EditServerRoom"] = ("Edit Server Room", "Serverraum bearbeiten"),
        ["EditServerRoomDescription"] = ("Update an existing server room in a dedicated form.", "Bearbeiten Sie einen bestehenden Serverraum in einem eigenen Formular."),
        ["CreateAgent"] = ("Create Agent", "Agent erstellen"),
        ["CreateAgentDescription"] = ("Create a new agent in a dedicated form.", "Erstellen Sie einen neuen Agenten in einem eigenen Formular."),
        ["EditAgent"] = ("Edit Agent", "Agent bearbeiten"),
        ["EditAgentDescription"] = ("Update an existing agent in a dedicated form.", "Bearbeiten Sie einen bestehenden Agenten in einem eigenen Formular."),
        ["CreateShellyDevice"] = ("Create Shelly Device", "Shelly-Geraet erstellen"),
        ["CreateShellyDeviceDescription"] = ("Create a new Shelly device in a dedicated form.", "Erstellen Sie ein neues Shelly-Geraet in einem eigenen Formular."),
        ["EditShellyDevice"] = ("Edit Shelly Device", "Shelly-Geraet bearbeiten"),
        ["EditShellyDeviceDescription"] = ("Update an existing Shelly device in a dedicated form.", "Bearbeiten Sie ein bestehendes Shelly-Geraet in einem eigenen Formular."),
        ["CreateMonitoredDevice"] = ("Create Monitored Device", "Ueberwachtes Geraet erstellen"),
        ["CreateMonitoredDeviceDescription"] = ("Create a new monitored device in a dedicated form.", "Erstellen Sie ein neues ueberwachtes Geraet in einem eigenen Formular."),
        ["EditMonitoredDevice"] = ("Edit Monitored Device", "Ueberwachtes Geraet bearbeiten"),
        ["EditMonitoredDeviceDescription"] = ("Update an existing monitored device in a dedicated form.", "Bearbeiten Sie ein bestehendes ueberwachtes Geraet in einem eigenen Formular."),
        ["CreateMaintenanceWindow"] = ("Create Maintenance Window", "Wartungsfenster erstellen"),
        ["CreateMaintenanceWindowDescription"] = ("Create a new maintenance window in a dedicated form.", "Erstellen Sie ein neues Wartungsfenster in einem eigenen Formular."),
        ["EditMaintenanceWindow"] = ("Edit Maintenance Window", "Wartungsfenster bearbeiten"),
        ["EditMaintenanceWindowDescription"] = ("Update an existing maintenance window in a dedicated form.", "Bearbeiten Sie ein bestehendes Wartungsfenster in einem eigenen Formular."),
        ["CustomerCreateFailed"] = ("Customer creation failed.", "Kundenerstellung fehlgeschlagen."),
        ["CustomerOpenIncidentsCount"] = ("{0} open incident(s) are currently being tracked.", "{0} offene Vorfaelle werden aktuell verfolgt."),
        ["CustomerPortfolioStable"] = ("No open incidents are currently active for this customer.", "Fuer diesen Kunden sind aktuell keine offenen Vorfaelle aktiv."),
        ["CustomerContactInfo"] = ("Contact: {0}", "Kontakt: {0}"),
        ["CustomerRoomsAgentsInfo"] = ("Rooms: {0} | Agents: {1}", "Raeume: {0} | Agenten: {1}"),
        ["NoContactInformation"] = ("No contact information", "Keine Kontaktinformationen"),
        ["LastActivityAt"] = ("Last activity: {0}", "Letzte Aktivitaet: {0}"),
        ["CustomerRoomsSectionDescription"] = ("The most important server rooms for this customer, including linked monitoring structure.", "Die wichtigsten Serverraeume dieses Kunden inklusive verknuepfter Monitoring-Struktur."),
        ["CustomerIncidentsSectionDescription"] = ("Newest incidents for this customer across all rooms and devices.", "Neueste Vorfaelle dieses Kunden ueber alle Raeume und Geraete hinweg."),
        ["CustomerAgentsSectionDescription"] = ("Preview of deployed agents and their most recent contact state.", "Vorschau der eingesetzten Agenten und ihres letzten Kontaktzustands."),
        ["CustomerShellySectionDescription"] = ("Preview of Shelly integrations and their most recent sensor snapshots.", "Vorschau der Shelly-Integrationen und ihrer letzten Sensorsnapshots."),
        ["CustomerMonitoredDevicesSectionDescription"] = ("Preview of monitored endpoints and their latest ping state.", "Vorschau der ueberwachten Endpunkte und ihres letzten Ping-Status."),
        ["CustomerMaintenanceSectionDescription"] = ("Upcoming and recent maintenance windows for this customer.", "Anstehende und aktuelle Wartungsfenster fuer diesen Kunden."),
        ["ServerRoomAgentsDescription"] = ("Agents deployed in this room and their linked device structure.", "In diesem Raum eingesetzte Agenten und deren verbundene Geraetestruktur."),
        ["ServerRoomMaintenanceDescription"] = ("Upcoming and recent maintenance windows for this room.", "Anstehende und aktuelle Wartungsfenster fuer diesen Raum."),
        ["ServerRoomIncidentsDescription"] = ("Latest incidents for this room.", "Neueste Vorfaelle fuer diesen Raum."),
        ["DashboardIncidentsDescription"] = ("Newest incidents across all customers.", "Neueste Vorfaelle ueber alle Kunden hinweg."),
        ["DashboardCustomersDescription"] = ("Customers with the highest current incident activity.", "Kunden mit der aktuell hoechsten Vorfallaktivitaet."),
        ["DashboardIncidentSummaryCritical"] = ("{0} of {1} open incidents are critical.", "{0} von {1} offenen Vorfaellen sind kritisch."),
        ["DashboardIncidentSummaryOpen"] = ("{0} open incidents are currently unresolved.", "{0} offene Vorfaelle sind aktuell ungeloest."),
        ["CustomerActivity"] = ("Customer Activity", "Kundenaktivitaet"),
        ["OpenIncidents"] = ("Open Incidents", "Offene Vorfaelle"),
        ["CriticalIncidents"] = ("Critical Incidents", "Kritische Vorfaelle"),
        ["ViewAll"] = ("View all", "Alle anzeigen"),
        ["LastLog"] = ("Last log", "Letztes Log"),
        ["NoPingYet"] = ("No ping result yet", "Noch kein Ping-Ergebnis"),
        ["Unavailable"] = ("Unavailable", "Nicht erreichbar"),
        ["PingHistory"] = ("Ping History", "Ping-Verlauf"),
        ["UnknownServerRoom"] = ("Unknown server room", "Unbekannter Serverraum"),
        ["UnknownAgent"] = ("Unknown agent", "Unbekannter Agent"),
        ["EditCustomer"] = ("Edit Customer", "Kunde bearbeiten"),
        ["EditCustomerDescription"] = ("Update the master data for this customer.", "Aktualisieren Sie die Stammdaten dieses Kunden."),
        ["CustomerSaveFailed"] = ("Customer update failed.", "Kundenaktualisierung fehlgeschlagen."),
        ["ExternalReference"] = ("External Reference", "Externe Referenz"),
        ["LatestReading"] = ("Latest Reading", "Letzte Messung"),
        ["NoReadingYet"] = ("No reading yet", "Noch keine Messung"),
        ["OpenChildren"] = ("Open children", "Unterelemente oeffnen"),
        ["RecentIncidents"] = ("Recent Incidents", "Aktuelle Vorfaelle"),
        ["ConfiguredShellyDevices"] = ("Configured Shelly Devices", "Konfigurierte Shelly-Geraete"),
        ["ConfiguredMonitoredDevices"] = ("Configured Monitored Devices", "Konfigurierte ueberwachte Geraete"),
        ["ConfiguredAgents"] = ("Configured Agents", "Konfigurierte Agenten"),
        ["ConfiguredServerRooms"] = ("Configured Server Rooms", "Konfigurierte Serverraeume"),
        ["ConfiguredMaintenanceWindows"] = ("Configured Maintenance Windows", "Konfigurierte Wartungsfenster"),
        ["MaintenanceTitle"] = ("Title", "Titel"),
        ["RecordedAt"] = ("Recorded At", "Erfasst am"),
        ["IncidentStatus"] = ("Incident Status", "Vorfallstatus"),
        ["Severity"] = ("Severity", "Schweregrad"),
        ["Status"] = ("Status", "Status"),
        ["OpenedAt"] = ("Opened At", "Geoeffnet am"),
        ["ResolvedAt"] = ("Resolved At", "Geloest am"),
        ["LastOccurredAt"] = ("Last Occurred At", "Zuletzt aufgetreten am"),
        ["CorrelationKey"] = ("Correlation Key", "Korrelationsschluessel"),
        ["TicketSyncStatus"] = ("Ticket Sync Status", "Ticket-Sync-Status"),
        ["TicketStatus"] = ("Ticket Status", "Ticket-Status"),
        ["TicketPriority"] = ("Ticket Priority", "Ticket-Prioritaet"),
        ["TicketLinks"] = ("Ticket Links", "Ticket-Verknuepfungen"),
        ["IncidentEvents"] = ("Incident Events", "Vorfallereignisse"),
        ["IncidentsPageDescription"] = ("Review incidents created from sensor readings and monitored-device failures.", "Pruefen Sie Vorfaelle aus Sensordaten und Ausfaellen ueberwachter Geraete."),
        ["BackToIncidents"] = ("Back to Incidents", "Zurueck zu Vorfaellen"),
        ["ExternalTicketId"] = ("External Ticket ID", "Externe Ticket-ID"),
        ["LastSyncAttemptAt"] = ("Last Sync Attempt At", "Letzter Sync-Versuch am"),
        ["LastMessage"] = ("Last Message", "Letzte Nachricht"),
        ["RedmineTicket"] = ("Redmine Ticket", "Redmine-Ticket"),
        ["ServerRoom"] = ("Server Room", "Serverraum"),
        ["ShellyDevice"] = ("Shelly Device", "Shelly-Geraet"),
        ["MonitoredDevice"] = ("Monitored Device", "Ueberwachtes Geraet"),
        ["DoorOpenOutsideMaintenanceWindow"] = ("Door Open Outside Maintenance Window", "Tuer ausserhalb des Wartungsfensters offen"),
        ["TemperatureWarningThresholdExceeded"] = ("Temperature Warning Threshold Exceeded", "Temperatur-Warnschwelle ueberschritten"),
        ["TemperatureCriticalThresholdExceeded"] = ("Temperature Critical Threshold Exceeded", "Temperatur-Kritikschwelle ueberschritten"),
        ["MonitoredDeviceFailureThresholdReached"] = ("Monitored Device Failure Threshold Reached", "Fehlerschwelle des ueberwachten Geraets erreicht"),
        ["Warning"] = ("Warning", "Warnung"),
        ["Major"] = ("Major", "Schwerwiegend"),
        ["Critical"] = ("Critical", "Kritisch"),
        ["Open"] = ("Open", "Offen"),
        ["Resolved"] = ("Resolved", "Geloest"),
        ["Closed"] = ("Closed", "Geschlossen"),
        ["PendingCreate"] = ("Pending Create", "Erstellung ausstehend"),
        ["Created"] = ("Created", "Erstellt"),
        ["Error"] = ("Error", "Fehler"),
        ["Low"] = ("Low", "Niedrig"),
        ["Normal"] = ("Normal", "Normal"),
        ["High"] = ("High", "Hoch"),
        ["Urgent"] = ("Urgent", "Dringend"),
        ["Immediate"] = ("Immediate", "Sofort"),
        ["New"] = ("New", "Neu"),
        ["InProgress"] = ("In Progress", "In Bearbeitung"),
        ["In Progress"] = ("In Progress", "In Bearbeitung"),
        ["Feedback"] = ("Feedback", "Rueckmeldung"),
        ["Rejected"] = ("Rejected", "Abgelehnt"),
        ["Reachable"] = ("Reachable", "Erreichbar"),
        ["Roundtrip"] = ("Roundtrip", "Laufzeit"),
        ["FailureCount"] = ("Failure Count", "Fehleranzahl"),
        ["FailureThreshold"] = ("Failure Threshold", "Fehlerschwelle"),
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
        ["LoggedInAs"] = ("You are logged in as", "Sie sind angemeldet als"),
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
        ["SelectAgent"] = ("Select agent", "Agent waehlen"),
        ["SelectCustomer"] = ("Select customer", "Kunden waehlen"),
        ["SelectServerRoom"] = ("Select server room", "Serverraum waehlen"),
        ["SelectShellyDevice"] = ("Select Shelly device", "Shelly-Geraet waehlen"),
        ["SelectMonitoredDevice"] = ("Select monitored device", "Ueberwachtes Geraet waehlen"),
        ["ApiKeyReference"] = ("API Key Reference", "API-Key-Referenz"),
        ["LastSeen"] = ("Last Seen", "Zuletzt gesehen"),
        ["DeviceType"] = ("Device Type", "Geraetetyp"),
        ["BaseUrl"] = ("Base URL", "Basis-URL"),
        ["MacAddress"] = ("MAC Address", "MAC-Adresse"),
        ["Firmware"] = ("Firmware", "Firmware"),
        ["Virtual"] = ("Virtual", "Virtuell"),
        ["WarningThreshold"] = ("Warning Threshold", "Warnschwelle"),
        ["CriticalThreshold"] = ("Critical Threshold", "Kritische Schwelle"),
        ["Start"] = ("Start", "Start"),
        ["End"] = ("End", "Ende"),
        ["IpAddress"] = ("IP Address", "IP-Adresse"),
        ["HostOrIpAddress"] = ("Host or IP Address", "Host oder IP-Adresse"),
        ["IntervalSeconds"] = ("Interval (s)", "Intervall (s)"),
        ["TimeoutMilliseconds"] = ("Timeout (ms)", "Zeitlimit (ms)"),
        ["Battery"] = ("Battery", "Batterie"),
        ["Brightness"] = ("Brightness", "Helligkeit"),
        ["Door"] = ("Door", "Tuer"),
        ["DoorOpenLabel"] = ("Door Open", "Tuer offen"),
        ["OpenRoom"] = ("Open Room", "Raum oeffnen"),
        ["RoomAgents"] = ("Room Agents", "Raum-Agenten"),
        ["AgentDevices"] = ("Agent Devices", "Agent-Geraete"),
        ["AgentMonitoredDevices"] = ("Agent Devices", "Agent-Geraete"),
        ["MonitoringInventoryDescription"] = ("Quick access to monitored asset lists across the platform.", "Schnellzugriff auf ueberwachte Bestandslisten der Plattform."),
        ["ServerRoomBoardDescription"] = ("Live room snapshot for temperature, door state, network reachability, and recent activity.", "Live-Raumansicht fuer Temperatur, Tuerstatus, Netzwerkerreichbarkeit und letzte Aktivitaet."),
        ["PingUnavailable"] = ("Unavailable", "Nicht erreichbar"),
        ["Yes"] = ("Yes", "Ja"),
        ["No"] = ("No", "Nein"),
        ["RotateSecretOptional"] = ("Leave empty to keep the current secret.", "Leer lassen, um das aktuelle Secret beizubehalten."),
        ["AdminPasswordReset"] = ("Administrative Password Reset", "Administratives Passwort-Reset"),
        ["ResetPassword"] = ("Reset Password", "Passwort zuruecksetzen"),
        ["ManagedPasswordResetSuccess"] = ("The managed user's password was reset.", "Das Passwort des verwalteten Benutzers wurde zurueckgesetzt."),
        ["PasswordPolicyHint"] = ("Password policy: at least 12 characters, including uppercase, lowercase, digit, and special character.", "Passwortrichtlinie: mindestens 12 Zeichen mit Grossbuchstaben, Kleinbuchstaben, Ziffer und Sonderzeichen."),
        ["MustChangePasswordNotice"] = ("Your password was reset by an administrator. You must set a new password before continuing.", "Ihr Passwort wurde von einem Administrator zurueckgesetzt. Sie muessen zuerst ein neues Passwort setzen.")
        ,
        ["ServerRoomMonitoring"] = ("Server Room Monitoring", "Serverraum-Monitoring")
        ,
        ["Temperature"] = ("Temperature", "Temperatur")
        ,
        ["DoorStatus"] = ("Door Status", "Tuerstatus")
        ,
        ["NetworkDevices"] = ("Network Devices", "Netzwerkgeraete")
        ,
        ["LastUpdate"] = ("Last Update", "Letzte Aktualisierung")
        ,
        ["DashboardLeadCritical"] = ("{0} of {1} open incidents are critical and require attention.", "{0} von {1} offenen Vorfaellen sind kritisch und erfordern Aufmerksamkeit.")
        ,
        ["DashboardLeadOpen"] = ("{0} open incidents are currently being tracked across {1} customers.", "{0} offene Vorfaelle werden aktuell ueber {1} Kunden hinweg verfolgt.")
        ,
        ["DashboardLeadOpenScoped"] = ("{0} open incidents are currently being tracked for your server rooms.", "{0} offene Vorfaelle werden aktuell fuer Ihre Serverraeume verfolgt.")
        ,
        ["DashboardLeadStable"] = ("Monitoring is active across {0} customers, {1} server rooms, and {2} agents.", "Das Monitoring ist ueber {0} Kunden, {1} Serverraeume und {2} Agenten aktiv.")
        ,
        ["DashboardLeadStableScoped"] = ("Monitoring is active across {0} server rooms and {1} agents.", "Das Monitoring ist fuer {0} Serverraeume und {1} Agenten aktiv.")
        ,
        ["ConfiguredCustomerRecords"] = ("{0} configured customer record(s)", "{0} konfigurierte Kundeneintraege")
        ,
        ["InactiveCustomersCount"] = ("{0} customer record(s) are inactive.", "{0} Kundeneintraege sind inaktiv.")
        ,
        ["RoomSummaryCritical"] = ("At least one room has critical incidents.", "Mindestens ein Raum hat kritische Vorfaelle.")
        ,
        ["RoomSummaryOpen"] = ("At least one room has open incidents.", "Mindestens ein Raum hat offene Vorfaelle.")
        ,
        ["RoomSummaryStable"] = ("All rooms currently look stable.", "Alle Raeume wirken aktuell stabil.")
        ,
        ["SomeRoomMonitoringDisabled"] = ("Monitoring is disabled for at least one room.", "Monitoring ist fuer mindestens einen Raum deaktiviert.")
        ,
        ["MonitoringDisabled"] = ("Monitoring is currently disabled.", "Monitoring ist aktuell deaktiviert.")
        ,
        ["MonitoringEnabled"] = ("Monitoring is enabled.", "Monitoring ist aktiviert.")
        ,
        ["ServerRoomConfigured"] = ("Server room is configured.", "Serverraum ist konfiguriert.")
        ,
        ["InactiveAgentsCount"] = ("{0} inactive agent(s)", "{0} inaktive Agent(en)")
        ,
        ["AllAgentsHaveReported"] = ("All active agents have reported.", "Alle aktiven Agenten haben Daten gemeldet.")
        ,
        ["AgentHasReported"] = ("The agent has reported.", "Der Agent hat Daten gemeldet.")
        ,
        ["AgentInactive"] = ("The agent is inactive.", "Der Agent ist inaktiv.")
        ,
        ["AgentNotReportedYet"] = ("The agent has not reported yet.", "Der Agent hat noch keine Daten gemeldet.")
        ,
        ["SensorTelemetryArriving"] = ("Sensor telemetry is arriving.", "Sensortelemetrie trifft ein.")
        ,
        ["NoOpenIncidents"] = ("No open incidents.", "Keine offenen Vorfaelle.")
        ,
        ["OpenIncidentsUnresolved"] = ("Open incidents are still unresolved.", "Offene Vorfaelle sind noch nicht geloest.")
        ,
        ["NoCriticalIncidents"] = ("No critical incidents.", "Keine kritischen Vorfaelle.")
        ,
        ["CriticalIncidentsImmediateReview"] = ("Critical incidents need immediate review.", "Kritische Vorfaelle muessen sofort geprueft werden.")
        ,
        ["BatteryUnknown"] = ("Battery: Unknown", "Batterie: Unbekannt")
        ,
        ["BatteryPercent"] = ("Battery: {0}%", "Batterie: {0}%")
        ,
        ["VirtualShellyConfigured"] = ("Virtual Shelly is configured.", "Virtueller Shelly ist konfiguriert.")
        ,
        ["ShellyConfigured"] = ("Shelly device is configured.", "Shelly-Geraet ist konfiguriert.")
        ,
        ["Upcoming"] = ("Upcoming", "Bevorstehend")
        ,
        ["Completed"] = ("Completed", "Abgeschlossen")
        ,
        ["MaintenanceScheduled"] = ("Maintenance window is scheduled.", "Wartungsfenster ist geplant.")
        ,
        ["MaintenanceInProgress"] = ("Maintenance window is in progress.", "Wartungsfenster laeuft.")
        ,
        ["MaintenanceFinished"] = ("Maintenance window has finished.", "Wartungsfenster ist beendet.")
        ,
        ["CustomerLeadCritical"] = ("{0} critical incident(s) are open across {1} server room(s).", "{0} kritische Vorfaelle sind ueber {1} Serverraeume hinweg offen.")
        ,
        ["CustomerLeadOpen"] = ("{0} open incident(s) are being tracked across this customer environment.", "{0} offene Vorfaelle werden in dieser Kundenumgebung verfolgt.")
        ,
        ["CustomerLeadStable"] = ("{0} server room(s), {1} agent(s), and {2} Shelly device(s) are currently configured.", "{0} Serverraum/raeume, {1} Agent(en) und {2} Shelly-Geraet(e) sind aktuell konfiguriert.")
        ,
        ["ServerRoomCriticalIssue"] = ("At least one server room has a critical issue.", "Mindestens ein Serverraum hat ein kritisches Problem.")
        ,
        ["ServerRoomOpenIssue"] = ("At least one server room has an open issue.", "Mindestens ein Serverraum hat ein offenes Problem.")
        ,
        ["ServerRoomsStable"] = ("Server rooms currently look stable.", "Serverraeume wirken aktuell stabil.")
        ,
        ["InactiveAgentsPresent"] = ("There are inactive agents.", "Es gibt inaktive Agenten.")
        ,
        ["AgentsNotReportedYet"] = ("Some agents have not reported yet.", "Einige Agenten haben noch nicht berichtet.")
        ,
        ["ShellyCriticalIncidentOpen"] = ("A Shelly-related critical incident is open.", "Ein Shelly-bezogener kritischer Vorfall ist offen.")
        ,
        ["ShellyDoorOpen"] = ("At least one Shelly currently reports an open door.", "Mindestens ein Shelly meldet aktuell eine offene Tuer.")
        ,
        ["ShellyNoReadingsYet"] = ("Some Shelly devices have no readings yet.", "Einige Shelly-Geraete haben noch keine Messwerte.")
        ,
        ["ShellyReportingNormally"] = ("Shelly devices are reporting normally.", "Shelly-Geraete melden normal.")
        ,
        ["InactiveShellyDevicesCount"] = ("{0} Shelly device(s) are inactive.", "{0} Shelly-Geraete sind inaktiv.")
        ,
        ["InactiveShellyDevicesPresent"] = ("At least one Shelly device is inactive.", "Mindestens ein Shelly-Geraet ist inaktiv.")
        ,
        ["MonitoredDeviceThresholdDown"] = ("A monitored device is down beyond the failure threshold.", "Ein ueberwachtes Geraet ist ueber die Fehlerschwelle hinaus ausgefallen.")
        ,
        ["MonitoredDeviceUnreachable"] = ("At least one monitored device is currently unreachable.", "Mindestens ein ueberwachtes Geraet ist aktuell nicht erreichbar.")
        ,
        ["MonitoredDeviceUnreachableSingular"] = ("The monitored device is currently unreachable.", "Das ueberwachte Geraet ist aktuell nicht erreichbar.")
        ,
        ["MonitoredDeviceNoHistory"] = ("Some monitored devices have no ping history yet.", "Einige ueberwachte Geraete haben noch keinen Ping-Verlauf.")
        ,
        ["MonitoredDevicesReachable"] = ("Monitored devices are reachable.", "Ueberwachte Geraete sind erreichbar.")
        ,
        ["MonitoredDeviceReachable"] = ("The monitored device is reachable.", "Das ueberwachte Geraet ist erreichbar.")
        ,
        ["ShowClosedIncidents"] = ("Show Closed Incidents", "Geschlossene Vorfaelle anzeigen")
        ,
        ["HideClosedIncidents"] = ("Hide Closed Incidents", "Geschlossene Vorfaelle ausblenden")
        ,
        ["InactiveMonitoredDevicesCount"] = ("{0} monitored device(s) are inactive.", "{0} ueberwachte Geraete sind inaktiv.")
        ,
        ["InactiveMonitoredDevicesPresent"] = ("At least one monitored device is inactive.", "Mindestens ein ueberwachtes Geraet ist inaktiv.")
        ,
        ["OpenIncidentsTracked"] = ("Open incidents are still being tracked.", "Offene Vorfaelle werden weiterhin verfolgt.")
        ,
        ["NoOpenIncidentsRemain"] = ("No open incidents remain.", "Es sind keine offenen Vorfaelle mehr vorhanden.")
        ,
        ["MaintenanceConfigured"] = ("Maintenance windows are configured.", "Wartungsfenster sind konfiguriert.")
        ,
        ["ServerRoomLeadCritical"] = ("Critical incidents are open in this server room.", "In diesem Serverraum sind kritische Vorfaelle offen.")
        ,
        ["ServerRoomLeadOpen"] = ("Open incidents are currently being monitored in this server room.", "In diesem Serverraum werden aktuell offene Vorfaelle ueberwacht.")
        ,
        ["ServerRoomLeadStable"] = ("{0} agent(s), {1} Shelly device(s), and {2} monitored device(s) are configured here.", "{0} Agent(en), {1} Shelly-Geraet(e) und {2} ueberwachte Geraet(e) sind hier konfiguriert.")
        ,
        ["NoSensorReadingsReceivedYet"] = ("No sensor readings have been received yet.", "Es wurden noch keine Sensormesswerte empfangen.")
        ,
        ["MonitoredDeviceUnreachableShort"] = ("At least one monitored device is unreachable.", "Mindestens ein ueberwachtes Geraet ist nicht erreichbar.")
        ,
        ["NoPingResultsReceivedYet"] = ("No ping results have been received yet.", "Es wurden noch keine Ping-Ergebnisse empfangen.")
        ,
        ["NoTemperatureReadingReceivedYet"] = ("No temperature reading received yet.", "Noch keine Temperaturmessung empfangen.")
        ,
        ["LatestReadingAt"] = ("Latest reading: {0}", "Letzte Messung: {0}")
        ,
        ["Unknown"] = ("Unknown", "Unbekannt")
        ,
        ["DoorOpen"] = ("OPEN", "OFFEN")
        ,
        ["DoorClosed"] = ("CLOSED", "GESCHLOSSEN")
        ,
        ["NoDoorStateReceivedYet"] = ("No door state received yet.", "Noch kein Tuerstatus empfangen.")
        ,
        ["LatestTelemetryOrIncidentActivity"] = ("Latest telemetry or incident activity.", "Neueste Telemetrie- oder Vorfallaktivitaet.")
        ,
        ["NoActivityRecordedYet"] = ("No activity recorded yet.", "Noch keine Aktivitaet erfasst.")
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

    public string Tf(string key, params object[] args)
        => string.Format(T(key), args);

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
