# Imaan Tracker Free Deployment

Deploy in this order:

1. Supabase: free PostgreSQL database
2. Render: ASP.NET Core API
3. Netlify: static web frontend

## Phase 1: Supabase Database

1. Go to <https://supabase.com> and create a new project.
2. Save your database password somewhere safe.
3. Open the project dashboard.
4. Go to Project Settings > Database > Connection string.
5. Copy a Postgres connection string.

Use the connection string as Render's `DATABASE_URL`.

If Supabase gives a password placeholder like `[YOUR-PASSWORD]`, replace it with your real database password.

## Phase 2: Render API

1. Push this project to GitHub.
2. Go to <https://render.com>.
3. Create a new Web Service.
4. Connect your GitHub repo.
5. Set runtime:

```text
Docker
```

6. Keep root directory empty.

7. Set Dockerfile path:

```text
Dockerfile
```

8. Do not use the old Native Runtime build/start commands. Docker builds the API from the Dockerfile.

Old Native Runtime commands will fail if Render selects Node:

```text
bash: line 1: dotnet: command not found
```

9. Add environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
=postgresql://postgres:[YOUR-PASSWORD]@db.ukgeqckwqnabtknqjgav.supabase.co:5432/postgres
Jwt__Key=replace_with_a_long_random_secret_key_at_least_32_chars
Jwt__Issuer=ImaanTrackerAPI
Jwt__Audience=ImaanTrackerMobile
```

10. Deploy.
11. Open:

```text
https://YOUR-RENDER-SERVICE.onrender.com/swagger/index.html
```

Test register, login, today, and complete prayer.

## Phase 3: Netlify Frontend

1. Open `src/ImaanTracker.Web/config.js`.
2. Replace the local URL with your Render API URL:

```javascript
window.IMAAN_API_BASE = "https://YOUR-RENDER-SERVICE.onrender.com/api";
```

3. Go to <https://app.netlify.com/drop>.
4. Drag and drop this folder:

```text
src/ImaanTracker.Web
```

5. Open the Netlify URL and test signup, login, and prayer completion.

## Local Test Before Deploy

Backend:

```powershell
cd C:\Users\RIYAZ\ImaanTracker\src
dotnet run --project .\ImaanTracker.API\ImaanTracker.API.csproj
```

Frontend:

Open:

```text
C:\Users\RIYAZ\ImaanTracker\src\ImaanTracker.Web\index.html
```

## Notes

- Render free services can sleep when unused, so the first request can be slow.
- The frontend must use the hosted Render URL after deployment.
- Keep `Jwt__Key` secret. Do not put it in frontend files.
