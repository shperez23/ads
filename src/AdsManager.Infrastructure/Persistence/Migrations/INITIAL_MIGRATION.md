# Initial migration (sugerida)

Ejecutar desde la raíz de la solución:

```bash
dotnet ef migrations add InitialCreate --project src/AdsManager.Infrastructure --startup-project src/AdsManager.API --output-dir Persistence/Migrations
```

Aplicar migración:

```bash
dotnet ef database update --project src/AdsManager.Infrastructure --startup-project src/AdsManager.API
```
