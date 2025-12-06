# CONTRIBUTING.md - Contributing to Stardew Valley: Living Roots

Welcome to the "Living Roots" project! We appreciate your interest in contributing to this agroecological mod for Stardew Valley. This guide details how you can participate in the development, following our methodologies and software engineering principles.

## Development Philosophy

Our goal is to build a robust, extensible, and easily maintainable mod. To achieve this, we adopt the following principles and methodologies:

*   **KISS (Keep It Simple, Stupid):** Start with the simplest possible solution to a problem. Avoid unnecessary complexity, such as creating wrapper interfaces when the original interface is sufficient (e.g., using `IMonitor` directly instead of creating `IMonitorWrapper`).
*   **YAGNI (You Ain't Gonna Need It):** Do not implement features you don't need *now*. Focus on what is essential for the current sprint. Avoid premature abstractions that don't provide immediate value (e.g., removing unused interfaces like `IModService`).
*   **DRY (Don't Repeat Yourself):** Avoid code duplication. If a logic is used in multiple places, abstract it into a reusable function or class.
*   **TDD (Test-Driven Development):** Write automated tests *before* writing production code. This ensures correctness and facilitates future refactoring.
*   **SOLID:** A set of five software design principles to make code more understandable, flexible, and maintainable:
    *   **S (Single Responsibility Principle):** Each class should have only one reason to change.
    *   **O (Open/Closed Principle):** Software entities (classes, modules, functions, etc.) should be open for extension, but closed for modification.
    *   **L (Liskov Substitution Principle):** Objects in a program should be replaceable with instances of their subtypes without altering the correctness of the program.
    *   **I (Interface Segregation Principle):** Clients should not be forced to depend on interfaces they do not use.
    *   **D (Dependency Inversion Principle):** High-level modules should not depend on low-level modules. Both should depend on abstractions.
*   **DDD (Domain-Driven Design):** We focus on the agroecology domain, using a "Ubiquitous Language" in the code (e.g., `LivingSoil`, `HeirloomSeed`, `CompanionPlanting`).
*   **SCRUM (Adapted for Solo Developer/Small Team):** We use an iterative and incremental development cycle to manage the feature backlog.
    *   **Product Backlog:** Prioritized list of all mod ideas and features.
    *   **Sprint:** Short development cycles (e.g., 1 week).
    *   **Sprint Planning:** At the beginning of each sprint, we define goals and tasks to be completed.
    *   **Daily Scrum:** Brief daily update on progress, plans, and impediments.
    *   **Sprint Review:** At the end of the sprint, we review completed work and test new features in the game.

## Development Environment Setup

To start developing "Living Roots," follow the steps below:

1. **Install the Game:** Have Stardew Valley (via Steam or GOG) installed on your machine.

2.  **Install SMAPI:** Download and install the [Stardew Modding API (SMAPI)](https://smapi.io/) for your operating system. Follow the official installation instructions.

3. **Install .NET SDK:** Install the [.NET SDK](https://dotnet.microsoft.com/download) (required: .NET 6). You can check the installed version with `dotnet --version`.


4.  **Configure VS Code:**
    *   Install the official Microsoft extension: `C#`.

5.  **Clone the Repository:**
    *   Fork the "Living Roots" repository on GitHub.
    *   Clone your fork to your local machine:
        ```bash
        git clone [your_fork_url]
        ```

6.  **Create the Mod Skeleton (if it doesn't exist yet):**
    *   Open the terminal in the root folder of your cloned repository.
    *   Create the project (if one doesn't exist yet): `dotnet new classlib -n LivingRoots`
    *   Enter the project folder: `cd LivingRoots`
    *   Add SMAPI dependencies:
        ```bash
        dotnet add package Pathoschild.Stardew.ModBuildConfig
        ```

7. **Configure Launch (Debug):**
    *   Edit the `LivingRoots.csproj` file (inside the `LivingRoots` folder).
    *   Add the path to your game folder (where `StardewValley.exe` is located). This will allow VS Code to automatically launch the game and your mod when debugging.
        ```xml
        <!-- For Windows -->
        <!-- <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley</GamePath> -->

        <!-- For Linux -->
        <!-- <GamePath>/path/to/your/stardew/valley/installation</GamePath> -->

        <!-- For macOS -->
        <!-- <GamePath>/path/to/your/stardew/valley/installation</GamePath> -->
        ```
        *Remember to replace `/path/to/your/stardew/valley/installation` with the actual path to your Stardew Valley installation.*

8.  **First Test ("Hello World"):**
    *   Rename `Class1.cs` to `ModEntry.cs` (if it exists).
    *   Replace the content of `ModEntry.cs` with the base code provided in the architecture documentation or the initial example.
    *   Open the `LivingRoots` folder in VS Code.
    *   Press `F5` (Start Debugging). VS Code should compile your mod, copy it to the game's `Mods` folder, and launch SMAPI. Check the SMAPI console for the mod loading message.

## Contribution Workflow

1. **Choose a Task:** Consult the [ROADMAP.md](ROADMAP.md) and the Product Backlog (usually managed via GitHub issues) to find a task to work on.
2.  **Create a Branch:** Create a new branch for your feature or bug fix from the `main` branch (or `develop`, if applicable):
    ```bash
    git checkout -b feature/your-feature-name
    ```
3. **Develop with TDD:** Write tests before implementing the functionality. Ensure your tests fail initially and pass after implementation.
4.  **Follow SOLID and DDD Principles:** Keep your code clean, modular, and aligned with the ubiquitous language of the agroecological domain.
5.  **Test in Game:** After implementing and testing your logic with unit tests, test the functionality in Stardew Valley to ensure everything works as expected.
6.  **Commit:** Make small, atomic commits with clear and descriptive messages.
7.  **Pull Request (PR):** Open a Pull Request to the `main` branch (or `develop`). Describe the changes in detail, the tests performed, and how the functionality can be verified.

## Best Practices

*   **Communication:** Use GitHub issues to discuss ideas, report bugs, and track progress.
*   **Documentation:** Keep the documentation updated, especially `ROADMAP.md` and `ARCHITECTURE.md`.
*   **Pixel Art:** If contributing visual assets, try to maintain the Stardew Valley pixel art style.

Thank you for being part of the "Living Roots" community!
