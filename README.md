# FifoWatch

A Windows desktop tool for live-monitoring FIFO buffers on Siemens S7-1500 / S7-1200 PLCs over the S7CommPlus protocol.

## Features

- Connect to a PLC with IP address, username, and password
- Browse the PLC variable tree to select the array tag that backs your FIFO
- Auto-detect the standard FIFO header fields (`NextIndexToRead`, `NextIndexToWrite`, `RecordsStored`, `MaxNrOfRecords`) from the same datablock
- Poll at a configurable interval (default 500 ms) and display only the active window of records
- When the FIFO is empty, the last-seen record is shown grayed out so context is not lost
- Live pointer readout (head / tail / stored / max) updated on every poll tick
- Right-click a row to copy the variable name or its value to the clipboard
- Last used IP address is remembered between sessions

## Requirements

- Windows 10 / 11 (x64)
- .NET Framework 4.7.2 or later
- OpenSSL DLLs in the application folder (included in the S7CommPlusDriver release for x64)
- A Siemens S7-1500 or S7-1200 PLC reachable over the network

## Building

Open `FifoWatch.sln` in Visual Studio 2019 or later and build the `Release | x64` configuration. The post-build step copies the required OpenSSL DLLs next to the executable.

## Usage

1. **Connect** — enter the PLC IP address, username, and password, then click **Connect**.
2. **Select the array tag** — click **Browse...** next to *Array (required)* and expand the datablock that contains your FIFO array. Select the array node (not a single element).
3. **Select header tags** — either click **Auto-detect header** to let FifoWatch find the standard pointer fields automatically, or browse each header field individually.
4. **Start polling** — set the desired interval in milliseconds and click **▶ Start**. The grid updates on every tick and shows only the records currently stored in the FIFO window.
5. **Stop** — click **■ Stop** at any time. Click any **Browse...** button to reconfigure without disconnecting.

### FIFO header fields

| Field | Meaning |
|---|---|
| `NextIndexToRead` | Array index of the oldest unread record (head pointer) |
| `NextIndexToWrite` | Array index where the next record will be written (tail pointer) |
| `RecordsStored` | Number of records currently in the FIFO |
| `MaxNrOfRecords` | Capacity of the FIFO array |

Only the array tag is strictly required. If header tags are not configured, all array elements are shown on every tick.

## License

This project uses [S7CommPlusDriver](https://github.com/thomas-v2/S7CommPlusDriver) which is licensed under GPLv2.
