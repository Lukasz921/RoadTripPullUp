### Runing
to run
first run your docker engine 
then 
docker compose up --build

your frontend is at 5173 port
http://localhost:5173





to run localy
first run db container
docker compose up -d db


then run backend
cd ./Api
dotnet restore
dotnet watch run


and frontend
cd ./frontend
npm install
npm run dev

