# Auditoría arquitectónica completa — AdsManager

## 1) Arquitectura actual

### Capas existentes
- **Presentación (API)**: controladores REST, middlewares, configuración auth/autorización, registro de jobs recurrentes y exposición de Swagger/Hangfire.
- **Aplicación (Application)**: servicios de casos de uso, interfaces/puertos, DTOs, validadores FluentValidation y modelo `Result<T>`.
- **Dominio (Domain)**: entidades de negocio, enums y contrato `ITenantScoped`.
- **Infraestructura (Infrastructure)**: EF Core (DbContext, configuraciones y migraciones), repositorios, seguridad (JWT/hash/cifrado), integraciones con Meta API, cache y background jobs.

### Patrones identificados
- **Arquitectura en capas tipo Clean/Onion (monolito modular)**.
- **Ports & Adapters ligero** (`Application.Interfaces` + implementaciones en `Infrastructure`).
- **Repository pattern** para acceso a datos por agregado (`CampaignRepository`, `AdSetRepository`, etc.).
- **Cross-cutting middleware** (trace, tenant, excepción global).
- **Background processing** con Hangfire para sync y reglas.
- **Multi-tenant por claim + global query filter**.

### Módulos implementados
- Auth + refresh token.
- RBAC por políticas.
- Campaigns, AdSets, Ads, AdAccounts.
- MetaConnections.
- Integración Meta Ads (lectura/escritura/sync).
- Insights, Reports y Dashboard.
- Rules engine base y ejecución programada.
- Audit logs + API logs.
- Métricas internas y cache en memoria.

---

## 2) Partes incompletas o parciales

- **Persistencia**: sólida en estructura, pero faltan estrategias operativas de retención/particionado para tablas de alto crecimiento (`ApiLogs`, `InsightDaily`, `RuleExecutionLogs`).
- **Repositorios**: existen, pero no hay una capa clara de especificaciones/paginación reusable para consultas de alto volumen.
- **Servicios**: cubren CRUD y sync base, faltan capacidades avanzadas de optimización y automatización de negocio.
- **Sincronización**: hay jobs por entidad, pero sin control distribuido de concurrencia (riesgo de solape en múltiples réplicas).
- **Auditoría**: existe `AuditLog`, pero no toda acción crítica parece auditarse de forma uniforme.
- **Seguridad**: falta rate limiting, lockout, MFA y hardening de exposición operativa (Swagger/Hangfire).
- **Optimización de consultas**: no hay paginación estándar ni contratos de query avanzados en listados.
- **Multi-tenant**: base correcta por claim, pero no hay evidencia de jerarquía avanzada de tenant/subtenant o partición física por tenant.

---

## 3) Funcionalidades faltantes para un Ads Manager completo

Aunque hay cobertura relevante, para acercarse a un Business Manager completo faltan:

- **AdSets management avanzado**: templates, clonación, presupuestos por reglas, validación de solapes de audiencias.
- **Ads management avanzado**: versiones de creatividades, biblioteca de assets, estados de revisión/moderación.
- **MetaConnection management enterprise**: health scoring, reconnect UX/API, rotación segura de secretos y trazabilidad completa de cambios.
- **AdAccounts storage enriquecido**: metadatos financieros (moneda, timezone operativo, límites), ownership y permisos granulares por usuario.
- **Campaign sync incremental robusto**: manejo explícito de borrados/archivados remotos, reconciliación y retry por lotes.
- **Insights sync enterprise**: ventanas deslizantes, backfill histórico parametrizable, deduplicación avanzada y retry selectivo.
- **Background jobs resilientes**: colas por prioridad, backoff configurable por tipo de error y exclusión mutua distribuida.
- **Rules engine avanzado**: condiciones compuestas, scheduling por regla, simulación dry-run, acciones multi-paso.
- **Campaign optimization**: recomendaciones automáticas, budget pacing, alertas inteligentes y aprendizaje sobre performance histórica.

---

## 4) Problemas de arquitectura detectados

- **Acoplamiento API↔servicios**: controladores repiten validación de tenant (`if !_tenantProvider... Unauthorized`) en casi todos los endpoints.
- **Dependencia directa fuerte de Meta API**: el servicio integra sync, mapping, resiliencia, logging y persistencia en una sola clase extensa.
- **DTO/errores**: patrón `Result<T>` consistente, pero falta estandarización formal tipo `problem+json` para errores HTTP.
- **Validaciones parciales**: hay validadores en auth/ads/adsets/rules, pero no cobertura homogénea en todos los contratos expuestos.
- **Gobernanza RBAC estática**: mapa rol-permiso hardcodeado, difícil evolución multi-tenant dinámica.

---

## 5) Problemas de seguridad

- **Access tokens externos**: se cifran, pero `Decrypt` hace fallback silencioso devolviendo texto original si falla el descifrado.
- **JWT**: configuración robusta base, pero sin esquema de rotación/versionado explícito de claves.
- **Endpoints sensibles sin antiabuso**: `register/login/refresh` carecen de rate limiting/lockout.
- **Exposición operativa**: Swagger y Hangfire habilitados globalmente; faltan restricciones por entorno/red.
- **Tenant control**: está basado en claims; correcto para baseline, pero requiere reforzar validaciones de pertenencia para escenarios complejos B2B enterprise.

---

## 6) Problemas de escalabilidad

- **Consultas potencialmente pesadas**: listados de insights y entidades sin paginación.
- **Cache no distribuida**: `IMemoryCache` limita el scale-out horizontal.
- **Sincronización intensiva**: loops por entidad/cuenta con riesgo de latencia acumulada y presión de DB en grandes volúmenes.
- **HttpClient**: se usa `IHttpClientFactory` correctamente, pero faltaría estrategia explícita multi-tenant de throttling/pooling y observabilidad de consumo por cuenta.

---

## 7) Endpoints que deberían existir (o reforzarse)

### Ya existen
- `api/adsets`, `api/adsets/{id}`
- `api/ads`, `api/ads/{id}`
- `api/meta/connections`
- `api/adaccounts`

### Recomendados faltantes para completitud
- `GET /api/adaccounts/{id}` y `PUT /api/adaccounts/{id}` (detalle/edición).
- `POST /api/adaccounts/{id}/link-meta-connection` (vinculación explícita).
- `POST /api/campaigns/{id}/duplicate` (operación frecuente real).
- `POST /api/adsets/{id}/duplicate`.
- `POST /api/ads/{id}/duplicate`.
- `GET /api/meta/connections/{id}/health` (health check puntual).
- `POST /api/meta/connections/{id}/reconnect` (flujo operacional explícito).
- `GET /api/sync/jobs` y `POST /api/sync/jobs/{jobName}/run` (operación controlada de sync).
- `GET /api/audit-logs` y `GET /api/api-logs` (solo admin, con filtros y paginación).

---

## 8) Entidades de base de datos (objetivo)

### Ya implementadas
- `Tenant`, `User`, `Role`
- `MetaConnection`
- `AdAccount`, `Campaign`, `AdSet`, `Ad`
- `InsightDaily`
- `AuditLog`, `ApiLog`
- `RefreshToken`
- `SyncCursor`, `SyncJobRun`
- `Rule`, `RuleExecutionLog`

### Sugeridas adicionales
- `Permission`, `RolePermission` (RBAC dinámico).
- `UserAdAccountPermission` (autorización granular por cuenta).
- `CampaignBudgetHistory` (histórico de cambios de presupuesto/estado).
- `CreativeAsset` (repositorio de creatividades).
- `SyncError` (errores de sync trazables y reintentables).
- `WebhookEvent` (si se incorpora ingestión near-real-time de Meta).

---

## 9) Jobs recomendados

### Ya existen
- `SyncCampaignsJob`
- `SyncAdSetsJob`
- `SyncAdsJob`
- `SyncInsightsJob`
- `RefreshMetaTokenJob`
- `RuleEvaluationJob`

### Recomendados para madurez
- `SyncAdAccountsJob` dedicado y recurrente.
- `BackfillInsightsJob` parametrizable por rango de fechas.
- `ReconcileDeletedEntitiesJob` (estado local vs remoto).
- `AuditRetentionJob` y `ApiLogRetentionJob`.
- `CacheInvalidationJob` para dashboards críticos.

---

## 10) Plan de implementación por fases

### FASE 1 — Arquitectura
1. Estandarizar errores (`problem+json`) y contratos API.
2. Extraer componentes de integración Meta (cliente, mapeo, persistencia) para reducir clase monolítica.
3. Consolidar middleware/filtros para evitar repetición de chequeo tenant en controladores.

### FASE 2 — Persistencia
1. Incorporar paginación/ordenamiento/filtros en endpoints de listado.
2. Agregar entidades de permisos granulares y tablas de histórico operacional.
3. Definir políticas de retención/archivado para tablas de crecimiento rápido.

### FASE 3 — Integración Meta
1. Endpoints de salud/reconexión de MetaConnections.
2. Reforzar manejo de tokens y rotación de secretos.
3. Mejorar trazabilidad de fallos externos por tipo y severidad.

### FASE 4 — Sincronización
1. Concurrencia distribuida para jobs (evitar ejecuciones solapadas).
2. Jobs de reconciliación y backfill.
3. Reintentos por lote y estrategias de retry selectivo.

### FASE 5 — Dashboard avanzado
1. Métricas derivadas y comparativas (WoW/MoM).
2. Alertas inteligentes y recomendaciones de optimización.
3. Segmentación multi-cuenta con agregaciones rápidas.

### FASE 6 — Optimización
1. Migrar cache a Redis distribuido.
2. Añadir OpenTelemetry/Prometheus + SLO/alertas.
3. Pruebas de carga y tuning de índices/planes de ejecución.

---

## 11) Diagrama de arquitectura objetivo

```text
[Frontend Angular]
        |
        v
   [API .NET]
        |
        v
    [Application Services]
        |
        v
     [Repositories]
        |
        v
     [PostgreSQL]
```

```text
[API .NET]
    |
    v
[MetaAdsService / MetaConnector Layer]
    |
    v
[Meta Marketing API]
```

### Vista ideal extendida
```text
Angular SPA
   |
   v
API Gateway / BFF (opcional)
   |
   v
AdsManager.API
   |------------------------------|
   v                              v
Application Services         Background Jobs (Hangfire)
   |                              |
   v                              v
Repositories / UoW          Sync Orchestrator + Rules Engine
   |                              |
   v                              v
PostgreSQL <----------------> Redis Cache
   |
   v
Observability Stack (Logs, Metrics, Traces)

AdsManager.API --> Meta Connector --> Meta Marketing API
```

---

## Resumen ejecutivo

El proyecto está **bien encaminado y funcionalmente avanzado** (campaigns, adsets, ads, adaccounts, metaconnections, reportes, dashboard, sync y reglas base). Para un nivel enterprise similar a Meta Business Manager, el mayor gap está en **hardening de seguridad, escalabilidad distribuida, madurez operativa de sincronización y capacidades avanzadas de optimización publicitaria**.
