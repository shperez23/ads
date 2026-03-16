# Observabilidad operacional

## Configuración mínima

Agregar en `appsettings`:

```json
"Observability": {
  "EnablePrometheus": true,
  "MetricsEndpoint": "/metrics"
}
```

- `EnablePrometheus`: habilita OpenTelemetry Metrics + exportador Prometheus.
- `MetricsEndpoint`: endpoint HTTP para scraping (`/metrics` por defecto).

## Métricas expuestas

- `http_request_duration_ms`
- `http_request_errors_total`
- `meta_api_latency_ms`
- `meta_api_errors_total`
- `sync_duration_ms`
- `cache_hits_total`
- `cache_misses_total`
- `rule_executions_total`
- `campaign_creation_total`

Todas se exponen vía el endpoint configurado en `MetricsEndpoint`.

## Trazabilidad

`TraceId` se mantiene en logs mediante `TraceContextMiddleware` + `SerilogRequestLogging` y se devuelve como header `X-Trace-Id`.
