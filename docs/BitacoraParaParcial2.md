# Bitácora de ClubSocial-2

Acá anoto los refactors y las decisiones que voy tomando, para armar el informe del parcial 2.

## Refactor 1: clasificación de errores del clima (devolución del parcial 1)

- **Problema:** ClimaService usaba EnsureSuccessStatusCode(), que lanza la misma excepción para un 404 y para "sin conexión". Quien los diferenciaba era el ViewModel, y mi informe decía que lo hacía el servicio.
- **Qué cambié:** ClimaService ahora clasifica el error y lanza una ClimaException con un TipoErrorClima. El ViewModel solo elige el mensaje.
- **Impacto:** cada clase tiene una sola responsabilidad y el servicio se puede probar con un HttpClient falso, sin Internet.

## Fase 1: capa de datos para categorías y precios con vigencia

- **Problema:** los precios cambian con el tiempo y los pagos ya registrados no pueden verse afectados.
- **Qué hice:** modelos Categoria y Precio (cada precio tiene VigenteDesde), repositorios con inyección de la conexión y pruebas de integración con SQLite en memoria.
- **Impacto:** el precio de una fecha pasada se obtiene siempre igual aunque suban los precios; las pruebas lo comprueban.