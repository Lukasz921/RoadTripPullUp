## Running the project

### Option 1: Full Docker setup

1. Start Docker Desktop / Docker Engine.
2. Run:
   ```bash
   docker compose up --build
   ```
3. Open the frontend:
   - http://localhost:5173

### Option 2: Local development 

Run only the database in Docker, and run API + frontend locally.

1. Start database:
   ```bash
   docker compose up -d db
   ```
2. Run backend:
   ```bash
   cd src/API
   dotnet restore
   dotnet watch run
   ```
3. Run frontend:
   ```bash
   cd src/frontend
   npm install
   npm run dev
   ```
   
