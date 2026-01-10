# C# Producer-Consumer (Monitor, Single-Slot Buffer)

A minimal concurrency lab project implementing the **producer–consumer** pattern in C# using `lock` + `Monitor.Wait()` / `Monitor.PulseAll()` over a **single-element shared buffer**.

The program starts:
- **2 producer threads** (writers) generating random integers in different ranges
- **1 consumer thread** (reader) that sums received values
- **1 stats/monitor thread** printing the buffer snapshot and thread states every second

Execution runs for **30 seconds**, then the application signals a graceful stop and prints the consumer’s final sum.

## What it demonstrates

- **Classic producer–consumer synchronization** with a bounded buffer of size **1**
- Blocking behavior:
  - producers block when the buffer is **full**
  - consumer blocks when the buffer is **empty**
- `Monitor.Wait()` + `Monitor.PulseAll()` used to coordinate access
- Safe shutdown via a shared `volatile` stop flag and waking interrupted/blocked threads

## How it works

### Shared resource: single-slot buffer
- `Value` is the buffer (`int?`) where `null` means *empty*, and a number means *full*.
- `Gate` is the lock object protecting access to `Value`.

### Put (producer side)
- Waits while the buffer is full (`Value != null`)
- Writes a value and wakes waiting threads

### Take (consumer side)
- Waits while the buffer is empty (`Value == null`)
- Reads and clears the buffer, then wakes waiting threads

### Producers
Two producers generate numbers with different ranges and sleep randomly:
- Producer #1: values in `[21..37]`, sleep `120–300 ms`
- Producer #2: values in `[1337..4200]`, sleep `120–300 ms`

### Consumer
- Repeatedly takes values and adds them to `sum`
- Sleeps `150–350 ms` between takes
- Prints final sum on exit

### Stats thread
Runs as a **background thread** and prints every second:
- current buffer value (`null` or number)
- thread states of both producers and the consumer

## Running the program

### Requirements
- .NET SDK (recommended: .NET 6+)

### Build & run
```bash
dotnet build
dotnet run
