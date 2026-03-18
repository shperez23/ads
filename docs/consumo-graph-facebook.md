# Informe técnico: consumo de `https://graph.facebook.com/`

## Resumen ejecutivo

El proyecto consume `https://graph.facebook.com/v19.0/` desde una integración propia de infraestructura orientada a **Meta Ads / Facebook Graph API**. El diseño actual sigue una arquitectura **por capas y responsabilidades separadas**: la API expone controladores HTTP, la capa de aplicación define contratos y casos de uso, la capa de infraestructura implementa clientes HTTP, persistencia y jobs, y la capa de dominio concentra entidades y estados del negocio.

En términos prácticos, el consumo a Graph API cubre cuatro capacidades principales:

1. **Validación de conexiones Meta** mediante `debug_token` y `me/permissions`.
2. **Renovación de token** mediante `oauth/access_token`.
3. **Operaciones de Ads** para cuentas, campañas, conjuntos de anuncios, anuncios e insights.
4. **Sincronización incremental** hacia la base local usando cursores por tenant, cuenta y tipo de entidad.

La implementación incluye controles operativos relevantes, como **retry**, **circuit breaker**, **timeout**, **auditoría**, **métricas** y **persistencia de logs API**. No obstante, también aparecen algunos riesgos funcionales y técnicos: el cliente fija la versión `v19.0`, usa `PostAsJsonAsync` para endpoints que habitualmente aceptan `application/x-www-form-urlencoded`, y el método `CreateAdAsync` publica sobre `ads` sin prefijo evidente de cuenta, lo que merece validación funcional.

## Arquitectura inferida del proyecto

### Tipo de arquitectura

La solución se comporta como una **arquitectura en capas con fuerte separación Application / Infrastructure / Domain / API**, cercana a un enfoque **Clean Architecture** o **arquitectura hexagonal pragmática**.

### Distribución de responsabilidades

- **`AdsManager.API`**
  - Expone endpoints REST.
  - Resuelve el tenant actual.
  - Aplica autorización, validaciones de entrada, versionado y middleware transversal.
- **`AdsManager.Application`**
  - Define interfaces (`IMetaAdsService`, `IMetaConnectionApiClient`).
  - Orquesta casos de uso y servicios de negocio.
  - Mantiene DTOs, validadores y contratos.
- **`AdsManager.Infrastructure`**
  - Implementa acceso HTTP a Graph API.
  - Implementa persistencia EF Core, repositorios, jobs y observabilidad.
  - Aloja la integración concreta con Meta.
- **`AdsManager.Domain`**
  - Modela entidades persistentes como `MetaConnection`, `Campaign`, `AdSet`, `Ad`, `AdAccount`, `InsightDaily` y `ApiLog`.

### Patrón de integración Meta

El patrón actual es consistente:

- La **capa API** invoca contratos de aplicación.
- La **capa Application** define interfaces y resuelve reglas de negocio de conexión.
- La **capa Infrastructure** implementa dos clientes diferenciados:
  - `MetaAdsService`: operaciones de negocio publicitarias.
  - `MetaConnectionApiClient`: validación y refresh de credenciales.

Este reparto es coherente con el resto del repositorio y evita mezclar detalles HTTP con los controladores o con el dominio.

## Componentes que consumen Graph API

## 1. Cliente de conexión: `MetaConnectionApiClient`

Este cliente utiliza `https://graph.facebook.com/v19.0/` como `BaseAddress` y encapsula dos operaciones técnicas de la conexión:

- **Validación de token**
  - `GET debug_token?input_token=...&access_token={appId}|{appSecret}`
- **Consulta de permisos concedidos**
  - `GET me/permissions?access_token=...`
- **Refresh de token**
  - `GET oauth/access_token?grant_type=fb_exchange_token&client_id=...&client_secret=...&fb_exchange_token=...`

Este cliente no contiene lógica de negocio del tenant: se centra en interacción técnica con la API de Meta y devuelve resultados simples al servicio de aplicación.

## 2. Cliente publicitario: `MetaAdsService`

`MetaAdsService` concentra el consumo operativo de anuncios e insights y también fija `https://graph.facebook.com/v19.0/` como base.

### Operaciones de lectura

- `GET me/adaccounts?fields=id,name,account_status,currency,timezone_name`
- `GET act_{adAccountId}/campaigns?fields=id,name,status,objective`
- `GET act_{adAccountId}/insights?...&level=campaign|adset|ad`
- `GET {campaignMetaId}/adsets?fields=id,campaign_id,name,status,daily_budget,billing_event,optimization_goal,targeting&updated_since=...`
- `GET {adSetMetaId}/ads?fields=id,adset_id,name,status,creative&updated_since=...`

### Operaciones de escritura

- `POST act_{adAccountId}/campaigns`
- `POST {campaignId}` para actualizar estado de campaña
- `POST act_{adAccountId}/adsets`
- `POST {adSetId}` para actualizar ad set o su estado
- `POST ads` para creación de anuncios
- `POST {adId}` para actualización o cambio de estado de anuncio

### Operaciones de sincronización

La sincronización local usa los mismos endpoints anteriores, pero añadiendo `updated_since` y persistiendo resultados en tablas propias. Hay cursores por:

- tenant
- cuenta publicitaria
- tipo de entidad (`Campaign`, `AdSet`, `Ad`, `Insight`)

Esto permite sincronización incremental y reduce lecturas completas repetidas.

## Flujo funcional extremo a extremo

## 1. Alta y administración de conexión Meta

El usuario registra una conexión Meta con `AppId`, `AppSecret`, `AccessToken`, `RefreshToken`, expiración y `BusinessId`. Los secretos se cifran antes de persistirse. Posteriormente, el servicio de aplicación valida el token y confirma permisos requeridos.

Permisos requeridos actualmente:

- `ads_management`
- `ads_read`
- `business_management`

Según el resultado, la conexión pasa a `Connected` o `Invalid`.

## 2. Exposición HTTP interna

La API publica endpoints para consumir Meta indirectamente, por ejemplo:

- `GET /api/v{version}/meta/ad-accounts`
- `GET /api/v{version}/meta/ad-accounts/{adAccountId}/campaigns`
- `POST /api/v{version}/meta/ad-accounts/{adAccountId}/campaigns`
- `POST /api/v{version}/meta/ad-accounts/{adAccountId}/adsets`
- `POST /api/v{version}/meta/ads`
- `GET /api/v{version}/meta/ad-accounts/{adAccountId}/insights`

El controlador no habla directamente con `HttpClient`; delega en `IMetaAdsService` y resuelve el `tenantId` desde `ITenantProvider`.

## 3. Importación y sincronización operativa

Además de los endpoints interactivos, hay jobs de sincronización en background que reutilizan `IMetaAdsService` para poblar y refrescar datos locales:

- campañas
- conjuntos de anuncios
- anuncios
- insights

La sincronización persiste el estado en entidades propias, lo que desacopla lectura analítica y consultas del dashboard respecto de la disponibilidad inmediata de Meta.

## 4. Renovación de credenciales

Existen dos rutas de refresh:

- **manual / aplicada al caso de uso**: desde `MetaConnectionService.RefreshTokenAsync`
- **automática / batch**: desde `RefreshMetaTokenJob`

Ambas registran auditoría, actualizan estado de salud de la conexión y guardan trazas en `ApiLogs`.

## Controles de resiliencia y operación

## 1. Resiliencia HTTP

La integración de Ads implementa tres mecanismos importantes:

- **Retry exponencial**: 3 reintentos con backoff `2^attempt` segundos.
- **Circuit breaker**: abre tras 5 fallos y espera 30 segundos.
- **Timeout**: 15 segundos por operación.

Los estados considerados transitorios son:

- `429 Too Many Requests`
- `5xx`
- fallos de red / cancelaciones / timeout

## 2. Observabilidad

La solución registra métricas específicas para Meta:

- `meta_api_latency_ms`
- `meta_api_errors_total`

Además, cada llamada persiste un `ApiLog` con:

- proveedor
- endpoint
- método
- request serializado
- response serializado
- código HTTP
- estado lógico
- duración
- trace id

Esto da una base razonable para troubleshooting, auditoría técnica y análisis posterior.

## 3. Auditoría

Los eventos de conexión y refresh se registran mediante `IAuditService`, lo cual es consistente con el modelo transversal del proyecto.

## Modelo de seguridad aplicado al consumo

## Secretos y tokens

El proyecto cifra antes de persistir:

- `AppSecret`
- `AccessToken`
- `RefreshToken`

El token de acceso se descifra justo antes de invocar Graph API. Este patrón es correcto para reducir exposición en base de datos, aunque la seguridad efectiva depende de la implementación de `ISecretEncryptionService` y de la gestión de claves del entorno.

## Multi-tenant

El consumo es explícitamente **tenant-aware**. Antes de consumir Meta, la mayoría de operaciones resuelve el `tenantId` actual y obtiene la conexión asociada desde la base. Esto ayuda a evitar fugas de credenciales entre clientes.

## Políticas y permisos internos

Los endpoints internos que exponen operaciones Meta están protegidos con políticas como:

- `AdAccountsManage`
- `CampaignsRead`
- `CampaignsWrite`
- `AdSetsWrite`
- `AdsWrite`
- `ReportsRead`
- `MetaConnectionsManage`

Esto no sustituye la seguridad de Meta, pero sí controla quién puede gatillar el consumo dentro del sistema.

## Hallazgos técnicos relevantes

## Hallazgo 1. Versión de API fijada en `v19.0`

La versión está hardcodeada en ambos clientes (`MetaAdsService` y `MetaConnectionApiClient`). Esto simplifica compatibilidad inicial, pero implica riesgo de obsolescencia cuando Meta retire o despriorice esa versión.

**Impacto:** medio.

**Recomendación:** mover la versión a configuración (`appsettings` o variables de entorno) para facilitar upgrades sin recompilar.

## Hallazgo 2. Refresh implementado como `GET oauth/access_token`

El flujo actual usa `fb_exchange_token`, lo que parece apuntar al intercambio de token largo, no necesariamente a un refresh token tradicional universal. El propio código ya reconoce escenarios de “reauthentication required”, lo cual es una señal de que el refresh no siempre estará soportado por el flujo vigente.

**Impacto:** medio/alto.

**Recomendación:** documentar claramente qué tipo de token espera el sistema y cuáles son los escenarios reales de renovación soportados por el producto.

## Hallazgo 3. Persistencia intensiva de request/response

Las respuestas completas se guardan en `ApiLogs`. Esto mejora soporte, pero puede elevar:

- consumo de almacenamiento
- exposición de datos sensibles
- complejidad de cumplimiento y retención

**Impacto:** medio.

**Recomendación:** sanitizar payloads sensibles y definir una política de retención específica para logs de integración.

## Hallazgo 4. `CreateAdAsync` publica en `POST ads`

La creación de anuncios usa el endpoint relativo `ads`, a diferencia de campañas y ad sets que usan rutas ligadas a la cuenta o al recurso padre. Esto podría ser correcto según el flujo exacto esperado por Meta, pero requiere verificación funcional porque el patrón es menos explícito que en los demás métodos.

**Impacto:** medio.

**Recomendación:** validar el endpoint en pruebas integradas reales y, si aplica, homogeneizar el patrón de construcción de URLs.

## Hallazgo 5. Uso de `PostAsJsonAsync`

Los cuerpos de escritura se mandan con `PostAsJsonAsync` serializando diccionarios. En muchas operaciones de Graph API los ejemplos de referencia suelen usar parámetros tipo formulario o querystring.

**Impacto:** variable, dependiente del endpoint.

**Recomendación:** validar si todos los endpoints usados aceptan `application/json` de forma consistente. Si no, cambiar a `FormUrlEncodedContent` para alinearse con el contrato real del proveedor.

## Hallazgo 6. Paginación no visible en lecturas

`GetDataAsync` asume una respuesta con `data` y no muestra manejo de paginación (`paging.next`, cursores remotos, etc.).

**Impacto:** alto en cuentas grandes.

**Recomendación:** incorporar paginación explícita para cuentas con volúmenes elevados de campañas, ad sets, ads o insights.

## Hallazgo 7. Selección de conexión Meta por tenant sin criterio adicional

`GetAccessTokenAsync` recupera la primera conexión Meta del tenant. Si en el futuro se permiten múltiples conexiones por tenant, la selección podría volverse ambigua.

**Impacto:** medio.

**Recomendación:** definir una conexión activa por tenant, por business o por cuenta publicitaria, y reflejarlo en el modelo de datos.

## Riesgos operativos

### Riesgos de disponibilidad

- Dependencia directa de límites y latencia de Meta.
- Posibles bloqueos temporales por `429`.
- Timeouts de 15 segundos pueden quedarse cortos en consultas pesadas de insights.

### Riesgos de consistencia

- La sincronización incremental depende de `updated_since` y de un cursor local; si hay desajustes temporales, podrían omitirse cambios marginales.
- `SyncInsightsAsync` actualiza cursor local al final del proceso, lo cual es correcto, pero una falla intermedia puede obligar a reprocesar ventanas completas.

### Riesgos de seguridad y cumplimiento

- Los logs podrían almacenar información sensible proveniente de Meta.
- La serialización completa de request/response exige revisar políticas de masking y retención.

## Recomendaciones priorizadas

## Prioridad alta

1. **Externalizar `BaseUrl` y versión de Graph API** a configuración.
2. **Verificar paginación real** en todas las lecturas de `data`.
3. **Revisar el contrato HTTP de escritura** para confirmar si `PostAsJsonAsync` es válido en todos los casos.
4. **Validar funcionalmente `CreateAdAsync`** con cuentas reales o mocks de contrato.

## Prioridad media

1. **Definir una política de sanitización de `ApiLogs`**.
2. **Seleccionar explícitamente la conexión Meta activa** por tenant.
3. **Documentar el flujo exacto de refresh** soportado por producto.
4. **Agregar alertas** sobre `meta_api_errors_total`, `429`, circuit breaker abierto y conexiones próximas a expirar.

## Prioridad baja

1. **Centralizar el catálogo de endpoints Meta** en constantes o configuración tipada.
2. **Agregar pruebas de integración contractuales** para los endpoints más críticos.
3. **Incorporar métricas por tipo de recurso** (`campaigns`, `adsets`, `ads`, `insights`) para facilitar observación.

## Conclusión

El consumo de `https://graph.facebook.com/` en este proyecto está bien encapsulado y sigue una estructura arquitectónica coherente con el resto de la solución. La integración ya dispone de controles maduros de resiliencia, trazabilidad y persistencia local, lo que la hace operativamente sólida para un primer ciclo de producto.

Las principales oportunidades de mejora no están en la separación de capas, que es correcta, sino en la **robustez del contrato con el proveedor**: versionado configurable, manejo de paginación, validación exacta del formato de escritura y revisión del flujo de refresh. Si se atienden esos puntos, la integración puede escalar con menor riesgo técnico y operativo.
