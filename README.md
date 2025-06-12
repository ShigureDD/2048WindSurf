# 2048 Game

A Windows desktop implementation of the popular 2048 puzzle game, built with .NET 8.0 and Windows Forms.

## Features

- Classic 2048 gameplay
- Smooth tile animations
- Score tracking
- Responsive design
- Self-contained executable (no installation required)

## System Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in the release package)
- Minimum resolution: 800x600 pixels

## Download and Run

### Option 1: Download Pre-built Release

1. Go to the [Releases](https://github.com/ShigureDD/2048WindSurf/releases) page
2. Download the latest `2048.7z` file
3. Extract the ZIP file to a folder of your choice
4. Run `2048.exe`

### Option 2: Build from Source

#### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git (optional)

#### Steps

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/2048.git
   cd 2048
   ```

2. Restore dependencies:
   ```
   dotnet restore
   ```

3. Build the solution:
   ```
   dotnet build --configuration Release
   ```

4. Run the application:
   ```
   dotnet run --configuration Release
   ```

   Or run the built executable directly:
   ```
   cd bin\Release\net8.0-windows
   .\2048.exe
   ```

## How to Play

- Use the **arrow keys** to move the tiles
- Tiles with the same number merge into one when they touch
- Reach the 2048 tile to win!
- The game ends when there are no more moves possible
- Press `N` to start a new game at any time

## Building a Release Version

To create a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be in `bin\Release\net8.0-windows\win-x64\publish\`.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.


## Acknowledgements

- Original game by Gabriele Cirulli
- .NET 8.0
- Windows Forms
- AI-assisted development with Cascade
