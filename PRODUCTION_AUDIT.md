# Auditoría técnica de estado — AdsManager backend

## 1) Arquitectura actual inferida

### Estilo arquitectónico
El proyecto implementa una **arquitectura por capas / clean modular monolith** con separación explícita:

- `AdsManager.API`: capa de presentación HTTP (controllers, middlewares, configuración auth/authorization, Swagger, Hangfire dashboard).
- `AdsManager.Application`: casos de uso (services), contratos (`Interfaces`), DTOs, validaciones, mapeos y modelo `Result<T>`.
- `AdsManager.Domain`: entidades, enums y contratos de dominio (`ITenantScoped`).
- `AdsManager.Infrastructure`: persistencia EF Core, migraciones, repositorios, integraciones Meta, seguridad, background jobs, cache y métricas.

### Patrones observados
- **DI + interfaces por puerto** entre aplicación e infraestructura.
- **Repositorio por agregado funcional** (`CampaignRepository`, `AdSetRepository`, etc.).
- **Cross-cutting middleware** (trace + manejo global de excepciones + resolución de tenant/user).
- **Jobs asíncronos** con Hangfire para sincronización de datos y evaluación de reglas.
- **Filtro global multitenant** en EF para entidades `ITenantScoped`.

## 2) Estado actual por capacidad solicitada

### Cobertura funcional (sí/no/parcial)

| Capacidad | Estado | Evidencia / nota rápida |
|---|---|---|
| Auth seguro | **Parcial alto** | JWT con validación fuerte + refresh tokens hasheados + BCrypt; falta hardening adicional (rate limiting, MFA, lockout). |
| Multi-tenant | **Sí (base sólida)** | Tenant/User resueltos por middleware y filtros globales EF por `TenantId`. |
| Campaigns | **Sí** | Controlador y servicio dedicados + sync + acciones de estado. |
| Adsets | **Sí** | Controlador/servicio y jobs de sync. |
| Ads | **Sí** | Controlador/servicio y jobs de sync. |
| Adaccounts | **Sí** | Controlador/servicio y sincronización. |
| Metaconnections | **Sí** | Gestión de conexión, validación de permisos y refresh de token. |
| Sync jobs | **Sí** | Jobs recurrentes Hangfire para campañas/adsets/ads/insights/tokens/reglas. |
| Dashboard | **Sí** | Endpoint dedicado + servicio con cache. |
| Reports | **Sí** | Endpoint insights + filtros por fechas/campaña/account. |
| Audit logs | **Sí** | Entidad + servicio de auditoría con `TraceId`. |
| API logs | **Sí (parcial)** | Persistencia de logs en integraciones Meta y refresh token job (no uniforme en toda llamada externa). |
| Caching | **Sí (inicial)** | `IMemoryCache` con claves por prefijo; listo para migrar a Redis. |
| RBAC | **Sí (estático)** | Políticas por permisos y mapa role→permission en código. |
| Rules engine base | **Sí** | Entidades/migración + RuleEvaluationJob + acciones base (pause/alert). |

## 3) Hallazgos por categoría

## Seguridad

### Fortalezas
- Validación estricta de JWT (issuer/audience/signing/lifetime, clock skew controlado).
- Secret key obligatoria en producción (falla startup si no está configurada).
- Password hashing con BCrypt.
- Refresh token almacenado hasheado (SHA-256 + comparación constant-time).
- Cifrado de secretos con Data Protection para access tokens externos.
- Políticas de autorización por permiso en endpoints críticos.

### Huecos para producción
1. **Sin rate limiting/bot protection** en `login`, `refresh`, `register`.
2. **Sin estrategia de lockout** por intentos fallidos.
3. **Sin MFA / step-up auth** para operaciones sensibles.
4. **Sin rotación/versionado explícito de claves JWT** (key rollover).
5. **Fallback riesgoso al desencriptar secretos**: si falla `Unprotect`, retorna el valor original (podría ocultar corrupción de datos).
6. **Swagger expuesto sin condicionarlo por entorno**.
7. **Hangfire dashboard expuesto** (aunque autorizado); falta endurecimiento de red/IP y SSO.

## Performance

### Fortalezas
- Uso consistente de `AsNoTracking` en queries de lectura.
- Cache para dashboard/reportes con TTL configurable.
- Índices en migraciones para consultas críticas (insights/logs/rules).
- Reintentos + timeout + circuit breaker en integración Meta.

### Huecos
1. **Cache local in-memory** no compartida entre instancias.
2. **Sin paginación estándar** en listados grandes (campaigns/adsets/ads/reports).
3. **Riesgo N+1 / loops de sync** por consultas por entidad dentro de iteraciones.
4. **Sin compresión HTTP explícita** ni políticas de tamaño de payload.

## Escalabilidad

### Fortalezas
- Jobs desacoplados por tipo de sync.
- Separación clara por capas facilita extraer servicios en el futuro.

### Huecos
1. **Estado de cache no distribuido** (bloquea scale-out real).
2. **No hay partición de colas Hangfire por prioridad/criticidad**.
3. **Sin control de concurrencia distribuida** para evitar ejecuciones solapadas en multi-réplica.
4. **Sin estrategia explícita de particionado/retención de tablas de alto crecimiento** (`ApiLogs`, `InsightDaily`, `RuleExecutionLogs`).

## Mantenibilidad

### Fortalezas
- Organización limpia por proyecto/capa.
- Contratos claros en `Application.Interfaces`.
- Convenciones de DTO/validators consistentes.

### Huecos
1. **RBAC hardcodeado** en mapa estático; difícil gobernanza por tenant.
2. **No se observan pruebas automatizadas** (unit/integration/e2e de API).
3. **No se observan health checks/readiness probes**.
4. **No se observa versionado de API** (v1/v2 formal) más allá de swagger doc name.

## Observabilidad

### Fortalezas
- Serilog con `TraceId` enriquecido.
- Middleware de excepción global con correlación.
- Métricas custom (`meta_api_latency_ms`, `sync_duration_ms`, etc.).
- Persistencia de `AuditLog` y `ApiLog`.

### Huecos
1. **No se ve exportador OpenTelemetry/Prometheus** listo para scraping.
2. **Sin SLO/alertas** formalizadas (latencia, error budget, backlog de jobs).
3. **Api logs no homogéneos** para todas las integraciones externas.
4. **Sin endpoint health/metrics documentado** para operaciones.

## Experiencia de integración frontend

### Fortalezas
- DTOs tipados y estructura uniforme con `Result<T>`.
- Swagger con esquema Bearer habilitado.
- Endpoints por dominio (`/campaigns`, `/adsets`, `/ads`, `/reports`, `/dashboard`, `/rules`).

### Huecos
1. **Contratos de error no estandarizados** (códigos semánticos, `problem+json`).
2. **Sin paginación/ordenamiento/filtros enriquecidos** en listados.
3. **Sin idempotency keys** para operaciones sensibles `POST/PUT`.
4. **Sin política CORS visible en `Program.cs`** (puede frenar integración SPA).

## 4) Confirmación puntual del checklist solicitado

- Auth seguro: **PARCIAL ALTO** (base sólida, faltan controles anti abuso y hardening avanzado).
- Multi-tenant: **SÍ**.
- Campaigns: **SÍ**.
- Adsets: **SÍ**.
- Ads: **SÍ**.
- Adaccounts: **SÍ**.
- Metaconnections: **SÍ**.
- Sync jobs: **SÍ**.
- Dashboard: **SÍ**.
- Reports: **SÍ**.
- Audit logs: **SÍ**.
- API logs: **SÍ (PARCIAL)**.
- Caching: **SÍ (BÁSICO)**.
- RBAC: **SÍ (ESTÁTICO)**.
- Rules engine base: **SÍ**.

## 5) Checklist final de producción (go-live)

### Seguridad
- [ ] Rate limit + protection anti brute-force en auth endpoints.
- [ ] Lockout progresivo + auditoría de intentos fallidos.
- [ ] MFA para cuentas admin y acciones críticas.
- [ ] Rotación de secretos/keys y estrategia de key rollover JWT.
- [ ] Endurecer Data Protection key ring (persistencia segura + rotación).
- [ ] Restringir Swagger/Hangfire por entorno/red/SSO.
- [ ] Revisión OWASP ASVS (mínimo nivel L2).

### Plataforma / operación
- [ ] Health checks (`/health/live`, `/health/ready`).
- [ ] Exportación de métricas (OTel/Prometheus) + dashboards.
- [ ] Alertas (errores 5xx, latencia p95, fallos Hangfire, circuit abierto).
- [ ] Política de retención y archivado para logs/insights.

### Datos / performance
- [ ] Cache distribuida (Redis) con invalidación por tenant.
- [ ] Paginación + orden + filtros en endpoints de listado.
- [ ] Pruebas de carga en reportes y sync jobs.
- [ ] Índices validados con plan de ejecución real en PostgreSQL.

### Calidad / entrega
- [ ] Tests unitarios de servicios críticos (auth, rules, reports).
- [ ] Tests de integración API + DB + auth/tenant filters.
- [ ] Contratos OpenAPI congelados y versionados.
- [ ] Pipeline CI/CD con quality gates (build, tests, análisis estático, migraciones).

## 6) Roadmap final de cierre

### Fase 1 (0–2 semanas) — Hardening crítico
1. Rate limiting + lockout en auth.
2. Cerrar exposición de Swagger/Hangfire (solo entornos permitidos).
3. Health checks + alertas mínimas de operación.
4. Estandarizar errores API (`problem+json`) y contrato frontend.

### Fase 2 (2–4 semanas) — Escala y confiabilidad
1. Migrar cache a Redis.
2. Paginación y filtros avanzados en listados/reportes.
3. Concurrencia controlada en jobs (single execution por tenant/job window).
4. Política de retención de `ApiLogs`, `AuditLogs`, `InsightDaily`.

### Fase 3 (4–6 semanas) — Gobernanza y calidad
1. Suite de tests automáticos (unit + integration + smoke).
2. RBAC configurable (tabla de permisos por rol/tenant).
3. Observabilidad completa (métricas exportables + trazas distribuidas).
4. Preparación de release checklist + runbooks de incidentes.

## 7) Veredicto de auditoría

El backend está en **estado funcional avanzado** y cubre la mayoría de capacidades core del negocio. No obstante, para declararlo **production-ready enterprise**, aún faltan controles de hardening de seguridad, observabilidad operativa completa, y capacidades de escalado horizontal (cache distribuida + disciplina de jobs + pruebas de carga).

**Conclusión:** listo para **pilot/staging robusto**; requiere el roadmap de cierre para **go-live de producción con riesgo controlado**.
