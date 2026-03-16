# AdsManager — Runbook de incidentes

## 0) Triaging rápido (primeros 10 minutos)

1. Verificar estado general:
   - `GET /health/live`
   - `GET /health/ready`
2. Revisar logs con `TraceId` y errores recientes.
3. Revisar métricas (`/metrics`) para:
   - `meta_api_errors_total`
   - `sync_duration_ms`
   - `http_request_errors_total`
4. Determinar alcance:
   - ¿Todos los tenants o solo algunos?
   - ¿Solo jobs o también API síncrona?

---

## 1) Incidente: falla Meta API

### Síntomas
- Incremento `meta_api_errors_total`.
- Jobs de sync fallando (`Sync*Job`, `RefreshMetaTokenJob`).
- Degradación en health summary de conexiones Meta.

### Diagnóstico
1. Confirmar si el problema es global (Meta outage) o de credenciales por tenant.
2. Revisar `ApiLogs` para proveedor `Meta` y endpoints fallidos.
3. Revisar estado de `MetaConnections`:
   - `Status`
   - `LastHealthCheckStatus`
   - `LastHealthCheckDetails`
   - `TokenExpiration`

### Mitigación inmediata
- Si es outage externo: comunicar incidente, reducir ruido de alertas, mantener reintentos controlados.
- Si son tokens expirados/invalidos: forzar proceso de re-autenticación del tenant afectado.
- Si falla refresh token: validar AppId/AppSecret/token guardado y estado `ReauthenticationRequired`.

### Recuperación
- Ejecutar jobs afectados de forma manual/forzada tras restaurar conectividad.
- Validar que los próximos ciclos recurrentes vuelvan a estado `Succeeded`.

---

## 2) Incidente: fallan jobs (Hangfire)

### Síntomas
- `/health/ready` con componente Hangfire en `Unhealthy`.
- No se actualizan campañas/adsets/ads/insights.
- Acumulación de jobs fallidos o en retry.

### Diagnóstico
1. Verificar conectividad PostgreSQL (storage Hangfire).
2. Revisar logs de `SyncOrchestratorService` y jobs específicos.
3. Verificar conflictos por concurrencia lógica (`IJobExecutionGuard`) si hay ejecuciones activas/solapadas.
4. Confirmar que recurrentes estén registrados después del último deploy.

### Mitigación inmediata
- Recuperar PostgreSQL primero.
- Reintentar jobs críticos en este orden sugerido:
  1) `RefreshMetaTokenJob`
  2) `SyncCampaignsJob`
  3) `SyncAdSetsJob`
  4) `SyncAdsJob`
  5) `SyncInsightsJob`
- Si hay backlog fuerte, procesar por tenant prioritario.

### Recuperación
- Confirmar `SyncJobRunStatus.Succeeded` en ejecuciones nuevas.
- Verificar que métricas de duración y error vuelvan a baseline.

---

## 3) Incidente: Redis caído (si Cache:Provider=Redis)

### Síntomas
- Aumento de latencia de endpoints que dependen de cache.
- Errores de cache distribuida (según implementación y logs).

### Diagnóstico
1. Validar conectividad al endpoint Redis.
2. Confirmar valor efectivo de `Cache:Provider` y `ADSMANAGER_REDIS_CONNECTION`.
3. Revisar si la instancia arrancó con fallback a `MemoryCache`.

### Mitigación inmediata
- Si Redis no se recupera rápido:
  - Cambiar temporalmente a `Cache:Provider=Memory` y redeploy controlado.
  - Advertir impacto: cache no compartida entre nodos.
- Evitar reinicios en masa sin coordinación (puede aumentar miss rate y carga DB).

### Recuperación
- Restaurar Redis.
- Volver a `Cache:Provider=Redis`.
- Monitorear hit/miss rate hasta estabilización.

---

## 4) Incidente: JWT secret mal configurado

### Síntomas
- La API no arranca en producción por `InvalidOperationException` de secret faltante/corto.
- O bien tokens inválidos al validar firma tras rotación no coordinada.

### Diagnóstico
1. Verificar variable `ADSMANAGER_JWT_SECRET` en runtime.
2. Verificar longitud real (>= 32 bytes) y que no tenga truncamientos/encoding incorrecto.
3. Verificar coherencia de secreto entre todas las réplicas.

### Mitigación inmediata
- Corregir secreto y reiniciar instancias gradualmente.
- Si hubo rotación inesperada: comunicar invalidación de sesiones y forzar relogin controlado.

### Recuperación
- Validar health check JWT en `/health/ready`.
- Ejecutar prueba de login + endpoint autenticado.

---

## 5) Escalamiento y comunicación

- Escalar a equipo de plataforma si hay indisponibilidad de PostgreSQL/Redis.
- Escalar a equipo de integraciones si persisten fallos Meta pese a credenciales válidas.
- Mantener bitácora del incidente con:
  - inicio/fin
  - tenants impactados
  - causa raíz
  - acción correctiva
  - acción preventiva
