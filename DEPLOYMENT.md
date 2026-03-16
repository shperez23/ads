# AdsManager — Runbook de despliegue (go-live mínimo)

## 1) Arquitectura operativa detectada

El proyecto está organizado en capas (`API`, `Application`, `Infrastructure`, `Domain`) con dependencia dirigida hacia dominio/aplicación, y la API actuando como composición de infraestructura + servicios. Para operación, esto implica:

- **API**: superficie HTTP, auth JWT, health checks, métricas, middleware.
- **Infrastructure**: PostgreSQL/EF Core, Hangfire, cache (Memory/Redis), integraciones Meta.
- **Application**: contratos, casos de uso, configuración funcional (retención, cache, observabilidad).
- **Domain**: entidades y enums de negocio.

## 2) Variables de entorno requeridas (mínimas)

> Priorizar variables de entorno en producción para secretos y conexiones.

### Críticas (bloquean arranque o auth)

- `ADSMANAGER_DB_CONNECTION`
  - Conexión PostgreSQL principal (también usada por Hangfire storage).
  - Ejemplo: `Host=...;Port=5432;Database=...;Username=...;Password=...`
- `ADSMANAGER_JWT_SECRET`
  - Secreto JWT (HMAC SHA-256).
  - **Mínimo 32 bytes**.
  - Si falta en producción, la app falla en startup.

### Opcionales recomendadas

- `ADSMANAGER_REDIS_CONNECTION`
  - Solo si `Cache:Provider=Redis`.
  - Si Redis no está disponible, el sistema puede degradar a `MemoryCache` según configuración/entorno.

### Configuración de appsettings obligatoria

- `Jwt:Issuer`
- `Jwt:Audience`
- `Cache:Provider` (`Memory` o `Redis`)
- `Features:*` (exposición de Swagger/Hangfire dashboard/ready health auth)
- `Observability:*` (`EnablePrometheus`, `MetricsEndpoint`)
- `DataRetention:*` (retención de logs/jobs/insights)
- `Cors:*` (orígenes y credenciales en prod)

## 3) Pre-requisitos de infraestructura

- **PostgreSQL** accesible desde la API.
- **Redis** accesible (solo si provider Redis).
- Persistencia para **DataProtection keys** (ver nota abajo).
- Reloj/NTP correcto en nodos (JWT + jobs + expiraciones).

## 4) Secuencia de despliegue (paso a paso)

1. **Configurar secretos y variables**
   - Inyectar `ADSMANAGER_DB_CONNECTION` y `ADSMANAGER_JWT_SECRET`.
   - Si aplica Redis, inyectar `ADSMANAGER_REDIS_CONNECTION` y `Cache:Provider=Redis`.

2. **Configurar appsettings de entorno**
   - Validar `Jwt:Issuer/Audience`.
   - Definir CORS productivo.
   - Definir `Features:HangfireDashboardEnabled=false` por defecto y habilitar solo con controles de acceso.

3. **Migraciones de base de datos**
   - La app ejecuta `Database.Migrate()` al iniciar.
   - Recomendación go-live: ejecutar despliegue en ventana controlada, y verificar schema antes de habilitar tráfico.
   - Verificar que el usuario de DB tenga permisos DDL para migraciones.

4. **Arranque de aplicación**
   - Levantar instancia(s) API.
   - Validar endpoints:
     - `/health/live`
     - `/health/ready`
     - endpoint de métricas (`/metrics` por defecto).

5. **Validar Hangfire**
   - Confirmar que el server de Hangfire inicia.
   - Confirmar creación/actualización de recurrentes:
     - sync campaigns/adsets/ads cada 6h
     - sync insights diario
     - refresh tokens Meta hourly
     - evaluación de reglas hourly
     - cleanup logs/jobs diario

6. **Smoke test funcional mínimo**
   - Login + emisión JWT.
   - Llamada autenticada a endpoint básico.
   - Confirmar escritura en DB y logs.

## 5) Notas operativas por componente

### PostgreSQL
- Fuente de verdad de negocio + storage Hangfire.
- Si PostgreSQL falla, fallan API de negocio, health check DB y ejecución de jobs.

### Migraciones
- Automáticas en startup (`Database.Migrate()`).
- Riesgo: arranque lento/fallido si hay lock o permisos insuficientes.

### Cache provider
- `Cache:Provider=Memory` para simplicidad.
- `Cache:Provider=Redis` para escenarios multi-instancia y coherencia de caché distribuida.
- Si Redis está mal configurado/no disponible, revisar fallback efectivo a Memory y su impacto (cache local por nodo).

### JWT secret
- Debe ser estable y rotado controladamente.
- Cambio no coordinado invalida tokens existentes.

### DataProtection keys
- Actualmente se registra `AddDataProtection()` sin proveedor explícito de persistencia externa.
- Para despliegue multi-réplica/rolling restart, **configurar persistencia compartida de keys** (filesystem compartido, DB, blob, etc.) para evitar invalidación de artefactos protegidos entre instancias.

### Redis (si aplica)
- Monitorear latencia y reconexiones.
- Configurar límites y timeouts del servidor Redis.
- Tener plan de degradación controlada a Memory si se decide operar sin Redis temporalmente.

### Hangfire
- Usa PostgreSQL como storage.
- Dashboard deshabilitado por defecto en producción salvo `Features:HangfireDashboardEnabled=true` y allowlist IP.
- Revisar cola, retries y jobs atascados post-deploy.
