# Operations Notes

Practical stuff for running/maintaining the deployed app — useful queries, manual steps that aren't automated (yet), that kind of thing. Not architecture reasoning (see `DECISIONS.md` for that).

## Applying a new EF Core migration to Azure SQL

There's no pipeline step that does this automatically — migrations are applied by hand after a deploy. From the repo root, in PowerShell:

```powershell
dotnet ef database update `
  --project BlackjackStreakSimulator.Data `
  --startup-project BlackjackStreakSimulator.Data `
  --connection "<Azure SQL connection string>"
```

Get the connection string from the Azure Portal → the **SQL Database** resource (not the App Service) → **Settings → Connection strings** → the ADO.NET one, with the real password filled in. First time connecting from a new machine, the SQL **server** resource's Networking firewall rules may need that machine's IP added.

Don't run this from Azure Cloud Shell — it has no access to the local repo or the `dotnet-ef` tool. Run it locally.

## Viewing visitor sessions in local time

`VisitorSessionEntry.StartedAtUtc` is stored in UTC. To see it in Mountain time when querying directly (e.g. in Azure Data Studio):

```sql
SELECT SessionId, StartedAtUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Mountain Standard Time' AS StartedAtLocal
FROM VisitorSessionEntry
ORDER BY StartedAtUtc DESC;
```

Handles the DST shift automatically. There's no way to auto-exclude Casey's own visits from this table (no IP/identity tracking, by design — see `DECISIONS.md`), so cross-reference against when you actually opened the app yourself.
