# McClarens

Aventura narrativa 2D con humor en Unity 6 / C#, inspirada en un bar real de Ponteareas (McClarens) y un grupo de amigos del desarrollador. El protagonista despierta encerrado en el bar y debe superar el reto de cada "Maestro" para poder salir. Duración objetivo: 1-2 horas. No es un RPG: sin niveles, sin combate, sin experiencia — la progresión es puramente narrativa.

## Regla de oro (innegociable)

Todo lo absurdo es completamente normal para los personajes. El protagonista es el único que se sorprende. Nunca romper esta regla en diálogos, diseño de niveles o escritura de personajes.

## Stack técnico

Unity 6, C#, 2D con perspectiva cenital ligeramente inclinada (estilo RPG moderno, no pixel art). Escenarios construidos con tilemaps + objetos + capas + partículas + luces — nunca una imagen de fondo única.

## Principios de desarrollo prioritarios

1. Terminar el juego antes que añadir sistemas complejos.
2. Todos los sistemas deben ser reutilizables (diálogos, logros, misiones, interacción).
3. Los diálogos deben estar separados de la lógica.

Lista completa en [Docs/Technical/DevPrinciples.md](Docs/Technical/DevPrinciples.md).

## Dónde buscar más contexto

| Archivo | Contenido |
|---|---|
| [Docs/GameDesign/Vision.md](Docs/GameDesign/Vision.md) | Pitch, tono, filosofía del absurdo y del humor |
| [Docs/GameDesign/Story.md](Docs/GameDesign/Story.md) | Historia principal, orden de progresión de maestros, final del juego |
| [Docs/GameDesign/Characters.md](Docs/GameDesign/Characters.md) | Camarero + los 5 maestros: personalidad, ubicación, qué entrega cada uno |
| [Docs/GameDesign/World.md](Docs/GameDesign/World.md) | Mapa del McClarens (hub, plantas, salas), televisiones |
| [Docs/GameDesign/Systems.md](Docs/GameDesign/Systems.md) | Sistema de diálogos y de logros |
| [Docs/Technical/EngineAndArt.md](Docs/Technical/EngineAndArt.md) | Motor, lenguaje, estilo visual, construcción de escenarios, género |
| [Docs/Technical/DevPrinciples.md](Docs/Technical/DevPrinciples.md) | Reglas de desarrollo a respetar en todo el proyecto |

### Sistema de diálogos (el sistema central del juego)

Los diálogos son lo más importante del proyecto. Tienen documentación propia en `Docs/DialogSystem/`:

| Archivo | Contenido |
|---|---|
| [Docs/DialogSystem/DialogSystem.md](Docs/DialogSystem/DialogSystem.md) | Visión y decisiones: cómo debe sentirse y qué debe hacer |
| [Docs/DialogSystem/ARQUITECTURA.md](Docs/DialogSystem/ARQUITECTURA.md) | Arquitectura técnica: clases, flujo, formato `.mcc`, puntos de extensión |
| [Docs/DialogSystem/ESTADO.md](Docs/DialogSystem/ESTADO.md) | Qué está hecho y qué falta (Tanda 1 / Tanda 2), mapa de archivos |
| [Docs/DialogSystem/README_USO.md](Docs/DialogSystem/README_USO.md) | Manual para escribir conversaciones en `.mcc` |

Antes de tocar el sistema de diálogos, lee `ARQUITECTURA.md` y `ESTADO.md`.

Consulta el archivo correspondiente antes de proponer contenido narrativo, personajes, escenarios o arquitectura de sistemas — no inventes datos que contradigan lo ya documentado.
