# NovelScraper

NovelScraper is a powerful and flexible command-line tool built with .NET 9 for scraping novels from various websites and converting them into beautifully formatted EPUB files. It provides options to generate a single EPUB for the entire novel or create separate EPUBs for each volume, complete with embedded Arabic-friendly fonts for a superior reading experience.

## Key Features

*   **Web Scraping**: Efficiently scrapes novel content, including chapter details and text, from supported websites.
*   **EPUB Generation**: Converts the scraped content into high-quality EPUB files using the QuickEPUB library.
*   **Flexible Output**:
    *   Generate a single, consolidated EPUB file for the entire novel.
    *   Generate separate EPUB files for each volume.
*   **Custom Font Embedding**: Embeds beautiful fonts like Alexandria and Markazi Text to ensure a pleasant and readable experience, especially for Arabic content.
*   **Clean Architecture**: Built using a layered architecture (Domain, Application, Infrastructure) which makes it easy to maintain, test, and extend with new features or additional website scrapers.
*   **Modern Tech Stack**: Utilizes .NET 9 and Playwright for robust and modern browser automation, ensuring compatibility with dynamic, JavaScript-heavy websites.

## How It Works

1.  **User Input**: The application prompts the user for the novel's URL, output directory, title, author, and volume range.
2.  **Browser Automation**: It uses **Playwright** to launch a headless browser and navigate to the provided URL.
3.  **Scraping**: The tool scrapes the table of contents to identify all volumes and their respective chapter links within the specified range.
4.  **Content Extraction**: It then navigates to each chapter page, extracts the novel text, and processes it.
5.  **EPUB Assembly**: The scraped content is passed to the **QuickEpubGenerator**, which assembles the chapters into volumes.
6.  **File Generation**: Finally, it generates the `.epub` files, embedding the custom fonts, and saves them to the specified output directory.

## Project Structure

The project follows the principles of **Clean Architecture** to create a separation of concerns, making the codebase more modular and maintainable.

*   `Domain`: Contains the core business logic and entities of the application, such as `Chapter`, `Volume`, and `Configuration`. It has no dependencies on external frameworks or technologies.
*   `Application`: Orchestrates the data flow and use cases, acting as a bridge between the Domain and Infrastructure layers. It contains logic for file system operations like creating directories.
*   `Infrastructure`: Contains all the external-facing components and implementations, such as:
    *   `BrowserService`: The implementation of the browser automation using **Playwright**.
    *   `Generators`: The **QuickEpubGenerator** responsible for creating the EPUB files.
    *   `Websites`: Concrete implementations for scraping specific websites (e.g., `KolNovel.cs`).

## Technologies Used

*   **.NET 9**
*   **Playwright**: For powerful and reliable browser automation.
*   **QuickEPUB**: For simple and efficient EPUB file generation.
*   **HtmlAgilityPack** (inferred): For parsing HTML content.

## Getting Started

### Prerequisites

*   .NET 9.0 SDK

### Usage

1.  Clone the repository:
    ```bash
    git clone https://github.com/TheMostafaOsamaDev/NovelScraper.git
    ```
2.  Navigate to the project directory:
    ```bash
    cd NovelScraper
    ```
3.  Run the application:
    ```bash
    dotnet run
    ```
4.  The console will prompt you for the following information:
    *   **Novel URL**: The URL of the novel's main page or table of contents.
    *   **Output Directory**: The local path where the EPUB files will be saved.
    *   **Novel Title**: The title of the book.
    *   **Author Name**: The author of the book.
    *   **Starting Volume**: The first volume number you want to scrape.
    *   **End Volume**: The last volume number you want to scrape.
    *   **Separated Volumes**: `yes` or `no` to determine if you want one file per volume.

The application will then begin the scraping process and save the resulting EPUB files in the specified directory.

## How to Contribute

Contributions are welcome! If you want to add a scraper for a new website or improve the existing functionality, please follow these steps:

1.  Fork the repository.
2.  Create a new branch for your feature (`git checkout -b feature/AddNewScraper`).
3.  Implement your changes. To add a new website, create a new class in the `Infrastructure/Websites` directory that inherits from a base website class or interface.
4.  Commit your changes (`git commit -m 'Add new scraper for WebsiteX'`).
5.  Push to the branch (`git push origin feature/AddNewScraper`).
6.  Open a Pull Request.
