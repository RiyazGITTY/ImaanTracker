# Imaan Tracker Web

Static MVP frontend for signup, login, today's five prayers, and marking prayers complete.

## Local test

Start the API first:

```powershell
cd C:\Users\RIYAZ\ImaanTracker\src
dotnet run --project .\ImaanTracker.API\ImaanTracker.API.csproj
```

Then open `index.html` in a browser, or serve the folder with any static server.

## Production API URL

The frontend uses `http://localhost:5263/api` by default. After hosting the API, update `config.js`:

```javascript
window.IMAAN_API_BASE = "https://YOUR-API-URL/api";
```
