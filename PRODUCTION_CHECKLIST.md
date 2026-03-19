# AdsManager — Checklist de producción (go-live)

## Seguridad

- [ ] `ADSMANAGER_JWT_SECRET` configurado, >=32 bytes, gestionado en secret manager.
- [ ] `Jwt:Issuer` y `Jwt:Audience` correctos para entorno productivo.
- [ ] `Cors:AllowedOrigins` restringido a dominios reales (sin comodines amplios).
- [ ] `Features:SwaggerEnabled=false` en producción (o protegido por auth/rol admin).
- [ ] `Features:HangfireDashboardEnabled=false` por defecto; si se habilita, usar allowlist IP.
- [ ] Revisión de secretos en `appsettings` (sin credenciales hardcodeadas para prod).
- [ ] HTTPS/TLS terminado correctamente en ingress/reverse proxy.
- [ ] Política de rotación de secretos (JWT y credenciales DB/Redis).

## Observabilidad

- [ ] `/health/live` y `/health/ready` accesibles según política de exposición.
- [ ] `Observability:EnablePrometheus=true` y scraping activo del endpoint de métricas.
- [ ] Alertas mínimas configuradas:
  - [ ] disponibilidad API
  - [ ] health check ready en estado unhealthy/degraded
  - [ ] aumento de `meta_api_errors_total`
  - [ ] aumento de `http_request_errors_total`
  - [ ] jobs fallidos/retries elevados
- [ ] Logs centralizados con `TraceId` y retención definida.

## Tests y validación previa

- [ ] Build y pruebas automáticas exitosas en pipeline.
- [ ] Smoke test post-deploy:
  - [ ] login JWT
  - [ ] endpoint autenticado
  - [ ] consulta a DB
  - [ ] ejecución de un job de sincronización
- [ ] Verificación de migraciones aplicadas sin errores.

## Performance y capacidad

- [ ] Baseline de latencia y throughput definido (p95/p99).
- [ ] Pool de conexiones PostgreSQL ajustado para carga esperada.
- [ ] Si multi-instancia, cache distribuida (Redis) evaluada/habilitada.
- [ ] Límites de recursos (CPU/Memoria) y autoscaling definidos.
- [ ] Rate limiting de auth validado según tráfico real.

## Backups y continuidad

- [ ] Backup automático de PostgreSQL habilitado y probado (restore test reciente).
- [ ] RPO/RTO acordados y documentados.
- [ ] Procedimiento de restauración validado en entorno no productivo.
- [ ] Plan ante caída de dependencia externa (Meta/Redis) documentado.

## Retención y housekeeping

- [ ] `DataRetention` ajustado a política legal/negocio:
  - [ ] `ApiLogsDays`
  - [ ] `AuditLogsDays`
  - [ ] `RuleExecutionLogsDays`
  - [ ] `SyncJobRunsDays`
  - [ ] `InsightDaily` (enabled/mode/tenant policies)
- [ ] Jobs de cleanup confirmados en Hangfire.
- [ ] Política de retención de logs de plataforma (app + infra) alineada.

## Go / No-Go final

- [ ] Todos los checks críticos en verde.
- [ ] On-call asignado para ventana de despliegue.
- [ ] Plan de rollback probado y disponible.
- [ ] Comunicación de release enviada a stakeholders.
