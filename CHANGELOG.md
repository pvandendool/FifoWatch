# Changelog

All notable changes to FifoWatch are documented here.

## [1.1.0] - 2026-05-04

### Added
- Multi-FIFO monitoring: any number of named FIFO monitors can be configured and watched simultaneously from a single window.
- Side-list layout: a monitor list on the left lets you switch between FIFOs; the right pane shows the live grid for the selected monitor.
- Add / Edit dialog: FIFO tag configuration (array, head, tail, count, max-records, auto-detect) moved to a dedicated dialog opened via **+ Add** or **Edit**, keeping the main window uncluttered.
- Global poll interval: one interval setting applies to all monitors; **▶ Start All** and **■ Stop All** start or stop polling for every monitor at once.
- Persistent monitor config: the list of configured monitors (tag names, access sequences, names) is saved to `%LOCALAPPDATA%\FifoWatch\monitors.xml` on close and restored on next launch.
- Per-tag array cache in PlcService: the symbol-resolution cache is now keyed per array tag name, so multiple active monitors no longer evict each other's cached variable lists.

## [1.0.1] - 2026-05-04

### Fixed
- Browse dialog silently failing when clicked while polling was active. Concurrent access to the driver connection from the poll timer thread and the UI thread corrupted the connection state, causing all subsequent Browse attempts to fail until the app was restarted. The poll timer now holds a semaphore for the duration of each read cycle, and the Browse dialog waits for the current cycle to finish before opening.

### Added
- Last used IP address is saved on connect and pre-filled on next startup.

## [1.0.0] - 2026-04-28

Initial release.

### Features
- Connect to Siemens S7-1500 / S7-1200 PLCs over S7CommPlus
- Variable browser with datablock tree and search filter
- Select array tag, head, tail, count, and max-records pointer tags independently
- Auto-detect standard FIFO header fields from the same datablock as the array
- Configurable poll interval with live grid display of active FIFO window
- Stale display: last-seen record shown in gray when the FIFO is empty
- Live pointer readout (NextIndexToRead / NextIndexToWrite / RecordsStored / MaxNrOfRecords)
- Struct array support: flattens scalar fields of each element into individual grid rows
- Byte/char array fields decoded and collapsed into a single readable string row
- Right-click context menu to copy variable name or value
