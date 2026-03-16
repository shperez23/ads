# Auditoría Arquitectónica de AdsManager

## 1) Arquitectura actual

### Capas identificadas
- **AdsManager.API**: capa de presentación HTTP con controladores, middlewares y composición de dependencias en `Program.cs`.
- **AdsManager.Application**: capa de aplicación con casos de uso (servicios), contratos (`Interfaces`), DTOs, validadores y mapeos.
- **AdsManager.Domain**: entidades de negocio, enums y contratos de alcance de tenant.
- **AdsManager.Infrastructure**: persistencia EF Core, repositorios, integración con Meta API, seguridad (JWT/hash/encriptación), jobs de Hangfire y observabilidad.

### Patrones utilizados
- **Arquitectura por capas (Clean-ish / Onion simplificada)** con separación API/Application/Domain/Infrastructure.
- **Repository Pattern** para Campaigns, AdAccounts, AdSets, Ads, Insights y MetaConnections.
- **Dependency Injection** en composición raíz.
- **DTO + Result wrapper** para respuestas de servicios.
- **Middleware pipeline** para trazabilidad, manejo de excepciones y tenant context.
- **Background processing** con Hangfire para sincronización periódica.
- **Integración resiliente** con Polly (retry/circuit breaker/timeout).

### Módulos implementados
- Autenticación/JWT/refresh token.
- Gestión de campañas, ad accounts, ad sets, ads y conexiones Meta.
- Reportes y dashboard.
- Persistencia multi-tenant con query filter global.
- Sync jobs de campañas/estructura e insights.
- Auditoría (`AuditLogs`) y logs de API externa (`ApiLogs`).

---

## 2) Partes incompletas o débiles

- **Validación funcional incompleta**: solo se observan validadores de Auth; faltan validadores para Campaigns/Ads/AdSets/Meta payloads.
- **Auditoría inconsistente**: se auditan operaciones en varios servicios, pero no existe un mecanismo transversal único (interceptor/domain event).
- **Sincronización parcial de insights**: actualmente persiste nivel campaña; no se completan dimensiones `AdSetId`/`AdId` ni métricas avanzadas.
- **Gobierno de secretos mejorable**: DataProtection local sin estrategia explícita de key ring distribuido.
- **Control fino de autorización (RBAC)**: hay claim de rol, pero no políticas por permiso/acción.
- **Optimización de escritura**: abundan `SaveChangesAsync` por operación pequeña (incluyendo logging), lo que incrementa roundtrips.
- **Gestión de errores de dominio**: se mezcla `Result.Fail` con excepciones genéricas, sin catálogo homogéneo de errores.

---

## 3) Funcionalidades faltantes para un Ads Manager “completo”

Aunque hay una base sólida, aún faltan capacidades enterprise:

- **AdSets management completo en API local**: faltan endpoints `POST /api/adsets`.
- **Ads management completo en API local**: faltan endpoints `POST /api/ads`.
- **MetaConnection lifecycle extendido**:
  - refresh automático de token,
  - health checks periódicos,
  - expiración proactiva.
- **Sync operativa avanzada**:
  - reintentos por cuenta con backoff por entidad,
  - re-sync incremental configurable por ventana,
  - detección de borrados/archivados en Meta.
- **Rules engine / optimization engine**:
  - reglas automáticas de pausa/activación,
  - alertas por KPIs,
  - recomendaciones de presupuesto/pujas.
- **Reporting avanzado**:
  - agregaciones por adset/ad,
  - comparativos period-over-period,
  - exportación (CSV/Excel) y programaciones.
- **Módulo de administración multi-tenant**:
  - onboarding de tenant,
  - límites/cuotas por plan,
  - feature flags por tenant.

---

## 4) Problemas de arquitectura detectados

- **Duplicación de lógica**:
  - `TryGetTenantId` repetido en múltiples servicios.
  - escritura de `AuditLog` repetida en Campaign/AdSet/Ads/AdAccount.
- **Acoplamiento directo a Meta API**:
  - servicios de aplicación dependen de operaciones Meta en tiempo real para mutaciones.
- **Inconsistencias de diseño API**:
  - existe `api/reports/dashboard` y `api/dashboard` con responsabilidad similar.
  - en AdSets/Ads no hay `POST` aunque los servicios sí lo soportan.
- **DTOs orientados a string statuses**: faltan value objects/enums de aplicación para blindar estados válidos.
- **Persistencia y side effects sin unidad transaccional explícita** en flujos que combinan cambios de negocio + auditoría + llamadas externas.

---

## 5) Problemas de seguridad

- **Refresh tokens en texto plano** (tabla de tokens), sin hashing para verificación segura.
- **JWT con secret efímero en development**: útil para dev, pero puede causar confusión operativa.
- **Dashboard Hangfire expuesto en la misma API** (aunque protegido por rol), recomendable endurecer con red privada/IP allowlist.
- **Autorización basada principalmente en “authenticated + tenant claim”** sin políticas granulares por recurso.
- **DataProtection sin evidencia de persistencia compartida de claves** para despliegues multi-instancia.

---

## 6) Problemas de escalabilidad

- **N+1 en sincronización**:
  - loops por campañas/adsets/ads con queries por elemento.
- **Persistencia muy frecuente**:
  - `SaveChangesAsync` repetidos en varios puntos del mismo flujo.
- **Sin caching de lecturas frecuentes**:
  - dashboard/reportes podrían beneficiarse de cache por tenant+rango.
- **HttpClient correcto por DI, pero falta rate limiting adaptativo por tenant/cuenta**.
- **Jobs monolíticos**:
  - `SyncCampaignsJob` ejecuta campañas+adsets+ads en cadena; convendría desacoplar y paralelizar controladamente.

---

## 7) Endpoints que deberían existir (o unificarse)

### Ya existen
- `GET /api/adsets`, `GET /api/adsets/{id}`, `PUT /api/adsets/{id}`
- `GET /api/ads`, `GET /api/ads/{id}`, `PUT /api/ads/{id}`
- `GET/POST/PUT/DELETE /api/meta/connections`
- `GET /api/adaccounts`

### Faltantes recomendados
- `POST /api/adsets`
- `POST /api/ads`
- `GET /api/meta/ad-accounts/{adAccountId}/adsets` (lectura directa Meta)
- `GET /api/meta/ad-accounts/{adAccountId}/ads` (lectura directa Meta)
- `POST /api/meta/connections/{id}/refresh-token`
- `POST /api/adaccounts/{id}/sync-insights`
- `GET /api/sync/jobs` y `GET /api/sync/jobs/{id}`

### A racionalizar
- Mantener **solo una** vía de dashboard (`/api/dashboard` o `/api/reports/dashboard`) para evitar contrato duplicado.

---

## 8) Entidades de base de datos

### Ya modeladas
- `Tenant`, `User`, `Role`, `RefreshToken`
- `MetaConnection`, `AdAccount`, `Campaign`, `AdSet`, `Ad`
- `InsightDaily`, `AuditLog`, `ApiLog`, `SyncCursor`, `SyncJobRun`

### Recomendadas adicionales
- `TenantSettings` (timezone/moneda/preferencias)
- `UserSession` (revocación centralizada de sesiones)
- `Rule` + `RuleExecutionLog`
- `Alert`/`Notification`
- `ReportExportJob`
- `RateLimitBucket` (opcional, para control de consumo de API externa)

---

## 9) Jobs recomendados

### Ya existen
- `SyncCampaignsJob`
- `SyncInsightsJob`

### Faltantes sugeridos
- `SyncAdSetsJob` (desacoplado)
- `SyncAdsJob` (desacoplado)
- `MetaConnectionHealthCheckJob`
- `RefreshMetaTokenJob`
- `RecalculateDashboardMaterializedJob`
- `RuleEvaluationJob`
- `CleanupApiLogsJob` / `CleanupAuditLogsJob`

---

## 10) Roadmap de implementación por fases

### FASE 1 – Arquitectura
- Unificar contratos de API (dashboard/reportes).
- Introducir policies RBAC por acción/recurso.
- Extraer auditoría a componente transversal.
- Definir catálogo de errores y ProblemDetails estándar.

### FASE 2 – Persistencia
- Hash de refresh token y rotación segura.
- Optimizar índices compuestos para queries de dashboard.
- Reducir `SaveChanges` por request (UoW por caso de uso).
- Agregar tablas para reglas/alertas/exportes.

### FASE 3 – Integración Meta
- Endpoints de lectura faltantes (adsets/ads vía Meta).
- Gestión de expiración/refresh de token de conexión.
- Versionado/configuración externa de campos solicitados a Meta.

### FASE 4 – Sincronización
- Separar jobs por entidad y particionar por tenant/adaccount.
- Backoff y control de concurrencia por proveedor.
- Reprocesos idempotentes y dead-letter para errores persistentes.

### FASE 5 – Dashboard avanzado
- KPIs por nivel campaign/adset/ad.
- series temporales y comparativos.
- exports y reportes programados.

### FASE 6 – Optimización
- cache por tenant+rango+filtros.
- pre-aggregations/materialized views para dashboard.
- alerting de performance/costos de API Meta.

---

## 11) Diagrama de arquitectura objetivo

```text
Frontend Angular
    ↓
API .NET (Controllers + Middleware + AuthZ)
    ↓
Application Services (Use Cases)
    ↓
Repositories / Query Services
    ↓
PostgreSQL
```

```text
API .NET
    ↓
MetaAdsService (resiliencia, logging, métricas)
    ↓
Meta Marketing API
```

### Vista recomendada extendida

```text
Angular SPA
   ↓
BFF/API .NET
   ├─ Auth/RBAC
   ├─ Campaign/AdSet/Ad Modules
   ├─ Reporting Module
   ├─ MetaConnection Module
   └─ Sync Orchestrator
         ↓
      Hangfire Workers
         ↓
      MetaAdsService → Meta Graph API

BFF/API .NET
   ↓
Application Layer
   ↓
Infrastructure
   ├─ EF Core Repositories
   ├─ Cache (Redis)
   ├─ Observability
   └─ Security services
   ↓
PostgreSQL
```
