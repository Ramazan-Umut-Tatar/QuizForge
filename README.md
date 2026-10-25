# QuizForge

QuizForge is a comprehensive vocabulary learning and memorization application built with C# and Windows Forms on .NET 8.0. The application is designed to optimize language learning through various testing methods, spaced repetition algorithms, and AI assistance.

## Core Features

* **Spaced Repetition System:** Utilizes a "Mastery Ladder" (`UstalikMerdiveni.cs`) algorithm to schedule word reviews based on user performance.
* **Multiple Study Modes:** Supports multiple-choice questions, written answers, and daily review sessions.
* **Mistake Targeting:** Includes a specific training mode (`CiftAntrenmanFormu.cs`) to help users practice words they frequently confuse.
* **Text-to-Vocabulary Extraction:** Features a module (`MetindenKelimeFormu.cs`) that extracts learnable words directly from user-provided texts.
* **AI Integration:** Integrates Claude AI (`ClaudeAiServisi.cs`) to assist with word validation and contextual learning.
* **Comprehensive Statistics:** Tracks user progress, session history, experience points (XP), and streaks using dedicated data models (`IstatistikDeposu.cs`, `Profil.cs`).
* **Fatigue Management:** Includes a break system (`MolaFormu.cs`) that monitors session length and suggests rests to prevent cognitive overload.
* **Pre-loaded Dictionaries:** Comes with built-in vocabulary lists for specific exams like YDS and YÖKDİL stored in the `/Dictionary` folder.
* **Customizable UI:** Features a dynamic theming engine (`TemaUygulayici.cs`) to adjust the visual appearance of the application.

## Project Structure

* **`/Configuration`**: Contains application settings management.
* **`/Controls`**: Houses custom user interface controls.
* **`/Data`**: Manages the persistence of statistics, JSON file handling, and the core vocabulary repository.
* **`/Dictionary`**: The default directory for raw `.txt` vocabulary files (e.g., YDS, Reading Power).
* **`/Forms`**: Contains all Windows Forms screens (Main Menu, Settings, Statistics, Quiz Sessions, Text Extractors).
* **`/Models`**: Defines the primary data structures, such as `KelimeKarti` (Word Card) and `OturumKaydi` (Session Record).
* **`/Services`**: Contains the business logic, AI connections, quiz session handlers, and spaced repetition logic.
* **`/UI` & `/Utilities`**: Provides UI helpers, theme applicators, and text manipulation tools.

## Getting Started

1. Clone or download the repository.
2. Open the `QuizForge.sln` solution file using Visual Studio 2022.
3. Ensure that the **.NET 8.0 SDK** is installed on your machine.
4. Build the solution. The `.txt` files in the `Dictionary` folder are configured to copy to the output directory automatically upon building.
5. Run the application to start a new learning session.




## Screen Shots

## Screen Shots

## Screen Shots

<p align="center">
  <img src="https://github.com/user-attachments/assets/b022808e-857b-48a5-995b-95183dc64399" width="500" title="Quiz Screen"><br><br>
  <img src="https://github.com/user-attachments/assets/f17aaf86-6753-4218-a53d-e8b4498caa03" width="500" title="Statistics Panel"><br><br>
  <img src="https://github.com/user-attachments/assets/77bd3c48-fd5e-485e-9fb5-9026418ffcc5" width="500" title="Multiple-Choice Questions"><br><br>
  <img src="https://github.com/user-attachments/assets/84f83352-18cd-4599-96e4-d42b83b581a3" width="500" title="Mixed Couples"><br><br>
  <img src="https://github.com/user-attachments/assets/4204fef3-f85c-40c2-80c7-5744ce523b13" width="500" title="The Mixed Couples Have Been Selected">
</p>
