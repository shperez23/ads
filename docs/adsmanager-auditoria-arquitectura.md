# Auditoría arquitectónica de AdsManager

## 1) Arquitectura actual

### Capas identificadas
- **Presentation/API** (`AdsManager.API`): controladores HTTP, middleware global, configuración de pipeline y Swagger.
- **Application** (`AdsManager.Application`): DTOs, contratos (interfaces), servicios de caso de uso, validaciones y mapeos.
- **Domain** (`AdsManager.Domain`): entidades, enums y contratos base (`ITenantScoped`, `BaseEntity`, `AuditableEntity`).
- **Infrastructure** (`AdsManager.Infrastructure`): EF Core (DbContext, configuraciones, migraciones, repositorios), seguridad (JWT/hash), integración Meta Ads, jobs de Hangfire.

### Patrones y estilo observados
- **Arquitectura en capas (Clean-ish/N-Tier)** con dependencia hacia adentro (API -> Application -> Domain; Infrastructure implementa contratos de Application).
- **Repository pattern** para Campaign, AdSet, Ad, Insight.
- **Service layer** para orquestación de reglas de negocio por módulo (`AuthService`, `CampaignService`, etc.).
- **DTO + Result wrapper** (`Result<T>`) para respuestas estandarizadas.
- **DI centralizada** en `AddInfrastructure(...)` y en `Program.cs`.
- **Background processing** con Hangfire y jobs periódicos de sincronización.

### Módulos implementados
- **Auth/JWT + refresh tokens**.
- **Campaign management** (listado, detalle, creación, update, pause/activate).
- **Meta Ads integration** (ad accounts, campaigns, adsets, ads, insights; sync de campañas/adsets/ads/insights).
- **Reportes/Dashboard** sobre tabla de insights.
- **Persistencia multientidad** para tenant, usuario, conexión Meta, ad account, campaign, adset, ad, insight, auditoría y API logs.

## 2) Partes incompletas del sistema

### Persistencia y repositorios
- Faltan repositorios explícitos para `MetaConnection`, `AdAccount`, `AuditLog`, `ApiLog`, `User/Tenant` en casos de uso no-auth (hoy se usa `DbContext` directo).
- `IApplicationDbContext` expone `InsightDaily` e `InsightsDaily` (duplicidad semántica).

### Servicios
- `IAdSetService` y `IAdsService` solo exponen **Create**, faltan CRUD y operaciones de estado.
- No existe un servicio explícito de **MetaConnection management** (alta/rotación/revocación/test conexión).
- No existe capa específica de **token lifecycle** de Meta (refresh proactivo, validación de expiración).

### Sincronización
- Sync actual orientado a campañas/adsets/ads/insights diarios, pero sin:
  - control de watermark/cursores por cuenta,
  - idempotencia robusta por lotes,
  - manejo de rate limit/backoff/reintentos con políticas centralizadas,
  - segmentación por prioridad.

### Auditoría/observabilidad
- Se guarda `AuditLog` y `ApiLog`, pero no hay correlación distribuida (trace id persistido en logs funcionales).
- No hay métricas técnicas (latencia por endpoint Meta, error ratio, throughput por tenant).

### Seguridad
- Se identifican secretos estáticos en `appsettings.json`.
- Tokens de Meta (`AccessToken`, `AppSecret`) parecen persistirse en claro.

### Optimización de consultas
- Dashboard materializa todos los insights (`ToListAsync`) antes de agregación; escala mal en volúmenes altos.

### Multi-tenant
- Hay `tenantId` en claims y entidades `ITenantScoped`, pero no hay **filtro global** de tenant a nivel DbContext ni aislamiento por política central.

## 3) Funcionalidades faltantes para un Ads Manager completo

- **AdSets management completo**: listar, detalle, editar, pausar/activar, eliminar lógico, clonación.
- **Ads management completo**: listar, detalle, editar estado, creatividad versionada, preview lifecycle.
- **MetaConnection management**: conectar/desconectar, health check, permisos, rotación de token.
- **AdAccounts storage/refresh**: endpoints para persistir cuentas conectadas y sincronizarlas.
- **Campaign sync avanzado**: incremental + full sync bajo demanda.
- **Insights sync avanzado**: granularidad diaria/horaria y backfill histórico.
- **Background jobs operativos**: colas por tenant, retry policies, dead-letter.
- **Rules/automation engine**: reglas de presupuesto, pausa por CPA/ROAS, alertas.
- **Optimization module**: recomendaciones automáticas y simulación de cambios.

## 4) Problemas de arquitectura detectados

- **Duplicación de middleware**: existen dos `GlobalExceptionMiddleware` en namespaces distintos.
- **Acoplamiento Application <-> Infrastructure vía DbContext**: servicios de aplicación consumen `IApplicationDbContext` y mezclan orquestación con persistencia/auditoría.
- **Dependencia directa fuerte a Meta API en servicios críticos**: creación de campañas/adsets/ads llama Meta en línea (sin outbox/cola).
- **Inconsistencias de DTOs de presupuesto** (`long?` en create vs `decimal?` en update) para Campaign.
- **Validaciones incompletas**: FluentValidation aplicado a Auth, pero no se observan validators equivalentes para Campaign/AdSet/Ad/Meta DTOs.
- **Diseño de endpoints parcial**: no hay controladores dedicados para CRUD de AdSets/Ads/MetaConnections/AdAccounts locales.

## 5) Problemas de seguridad

- **Secretos hardcodeados en configuración** (connection string y JWT key).
- **`/hangfire` expuesto sin autorización explícita**.
- **Persistencia de secretos/tokens sensibles sin cifrado aparente** en `MetaConnection`.
- **Respuesta de errores puede exponer detalles internos** (`details = ex.Message` en un middleware).
- **Controles de tenant distribuidos en controladores** (riesgo de olvido en endpoints nuevos).

## 6) Problemas de escalabilidad

- Agregaciones in-memory en Dashboard.
- `SaveChangesAsync` múltiples veces por flujo (repos + logs) en servicios transaccionales.
- Falta de caché para catálogos y lecturas frecuentes (ad accounts/campaign snapshots/dashboard).
- Integración Meta sin políticas explícitas de resiliencia (`Polly`) para throttling/errores transitorios.
- Jobs secuenciales por tenant/cuenta sin paralelismo controlado.

## 7) Endpoints que deberían existir y no existen

### AdSets
- `GET /api/adsets`
- `GET /api/adsets/{id}`
- `PUT /api/adsets/{id}`
- `PUT /api/adsets/{id}/pause`
- `PUT /api/adsets/{id}/activate`

### Ads
- `GET /api/ads`
- `GET /api/ads/{id}`
- `PUT /api/ads/{id}`
- `PUT /api/ads/{id}/pause`
- `PUT /api/ads/{id}/activate`

### MetaConnections
- `GET /api/meta/connections`
- `POST /api/meta/connections`
- `PUT /api/meta/connections/{id}`
- `DELETE /api/meta/connections/{id}`
- `POST /api/meta/connections/{id}/validate`

### AdAccounts
- `GET /api/adaccounts`
- `POST /api/adaccounts/import-from-meta`
- `POST /api/adaccounts/{id}/sync`

## 8) Entidades de base de datos recomendadas (target)

Base mínima (ya mayormente cubierta):
- Tenant
- User
- Role
- RefreshToken
- MetaConnection
- AdAccount
- Campaign
- AdSet
- Ad
- InsightDaily
- AuditLog
- ApiLog

Recomendadas adicionales:
- `SyncCursor` (por tenant + cuenta + tipo de entidad)
- `SyncJobRun` (histórico de ejecuciones y errores)
- `Rule` / `RuleExecution`
- `Recommendation`
- `WebhookEvent` (si se integra callback Meta)
- `FeatureFlag` (por tenant)

## 9) Jobs recomendados

- `SyncAdAccountsJob`
- `SyncCampaignsJob`
- `SyncAdSetsJob`
- `SyncAdsJob`
- `SyncInsightsJob`
- `BackfillInsightsJob`
- `RefreshMetaTokenJob`
- `EvaluateOptimizationRulesJob`
- `BuildDashboardSnapshotJob`

## 10) Roadmap de implementación por fases

### FASE 1 — Arquitectura
1. Definir bounded modules: Auth, MetaConnection, AdAccounts, Campaigns, AdSets, Ads, Insights, Reporting.
2. Unificar manejo de errores (un solo middleware).
3. Introducir políticas transversales: tenant resolution, authorization policy, observabilidad.
4. Endurecer contratos de DTO/validación por módulo.

### FASE 2 — Persistencia
1. Crear repositorios faltantes + Unit of Work transaccional.
2. Añadir tablas `SyncCursor`, `SyncJobRun`, `Rule`, `Recommendation`.
3. Añadir índices compuestos para consultas de dashboard/insights.
4. Cifrado en reposo para secretos de Meta.

### FASE 3 — Integración Meta
1. Módulo `MetaConnectionService` (connect/test/refresh/disconnect).
2. Resiliencia HTTP (retry, circuit breaker, backoff por rate limit).
3. Normalizar mapeos Meta DTO -> dominio.
4. Endpoints para importar y reconciliar AdAccounts.

### FASE 4 — Sincronización
1. Jobs por entidad con cursores incrementales.
2. Re-ejecución idempotente y trazabilidad de corridas.
3. Soporte backfill histórico configurable.
4. Alertas de fallos operacionales.

### FASE 5 — Dashboard avanzado
1. Preagregaciones/materialized views por ventana temporal.
2. Métricas por nivel (Account/Campaign/AdSet/Ad).
3. Reportes exportables y segmentaciones avanzadas.

### FASE 6 — Optimización
1. Rules engine (presupuesto, pausas automáticas, caps).
2. Recomendaciones automáticas (CTR/CPC/CPA).
3. Experimentación A/B y score de rendimiento.

## 11) Diagrama de arquitectura ideal

```text
[Frontend Angular]
        |
        v
[API .NET (Controllers + Auth + Tenant Policy)]
        |
        v
[Application Services / Use Cases]
        |
        +--------------------+
        |                    |
        v                    v
[Repositories/UoW]      [MetaAdsService + Resilience]
        |                    |
        v                    v
[PostgreSQL]         [Meta Marketing API]

Background:
[Hangfire Jobs] -> [Sync Services] -> [Repositories + MetaAdsService] -> [DB + Meta]
```
