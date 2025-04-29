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
  
  1. Create a text file named `client_events.jsonl` with user events, in aformat where each line is a valid JSON separated with new line:
   ```
   { "userId": "user1", "name": "add_revenue", "value": 60 } 
   { "userId": "user2", "name": "add_revenue", "value": 10 } 
   { "userId": "user2", "name": "add_revenue", "value": 10 } 
   ```

  2. Save it under FinonexClient project folder to:  
     ```
     FinonexClient\bin\Debug\net8.0\
     ```
  3. Run the `FinonexClient` project — this will upload the events file to the server.

  4. When FinonexClient finishes:
     - You got a prompt of the failed events and the total events uploaded to server.
     - The file renamed to have "Done" extention: client_events.jsonl.Done


- Run the `FinonexDataProcessor`:
  
  1. Create a file named `server_events.jsonl` with user events.
  2. Save it under FinonexDataProcessor project folder to:  
     ```
     FinonexDataProcessor\bin\Debug\net8.0\
     ```
  
  3. Run the `FinonexDataProcessor` project:

     - Follow the console prompts to enter the filename and processing options (1- user events bulk 2- single events):
       First prompt:
       ```
       Enter the file name (or empty for default 'server_events.jsonl'):
       ```
       Second prompt:
       ```
       Processing option ('1' per user events, '2' per single events):
       ```
              
       
     - The processor will insert the events into the PostgreSQL database.
     - When FinonexDataProcessor finishes:
       You got a prompt of "Done".


