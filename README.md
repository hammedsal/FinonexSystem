# FinonexSystem

A deme financial system built with C# and PostgreSQL

- FinonexClient: upload user events file to server.
- FinonexServer: save user event in local file, and reteive user events from database.
- FinonexDataProcessor: insert user events file on server to the database.

# Installation
1- Clone the Repository :
   ```bash
   git clone https://github.com/hammedsal/FinonexSystem.git
   cd FinonexSystem
   ```

2- Set Up the Database:
   - Install PostgreSQL.
   - Run the `db.sql` script on your local PostgreSQL server to create the necessary schema.

  
3- Build the Solution:
   - Open the solution in Visual Studio.
   - Restore NuGet packages.
   - Build the entire solution.

# Usage
- Start the `FinonexServer` project to launch the backend api.

- Run the `FinonexClient`:
  1. Create a file named `client_events.jsonl` with user events.
  2. Save it under FinonexClient project folder to:  
     ```
     FinonexClient\bin\Debug\net8.0\
     ```
  3. Run the `FinonexClient` project — this will upload the events file to the server.
  

- Run the `FinonexDataProcessor`:
  create "server_events.jsonl" with user events, and save it to \FinonexDataProcessor\bin\Debug\net8.0 folder.
  run FinonexDataProcessor project, answer the prompt for filename, and processing option.
  1. Create a file named `server_events.jsonl` with user events.
  2. Save it under FinonexDataProcessor project folder to:  
     ```
     FinonexDataProcessor\bin\Debug\net8.0\
     ```
  
  3. Run the `FinonexDataProcessor` project.  
     - Follow the console prompts to enter the filename and processing options (1- user events bulk 2- single events).  
     - The processor will insert the events into the PostgreSQL database.
