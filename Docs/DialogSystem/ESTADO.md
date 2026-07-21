# Sistema de Diálogos — Estado de implementación

> Documento de seguimiento. Refleja **qué está construido** y **qué falta**.
> La visión y filosofía están en [DialogSystem.md](DialogSystem.md). El manual de uso, en [README_USO.md](README_USO.md).

Última actualización: **2026-07-21**. Tanda 1 ya confirmada funcionando en el Editor (jugada de principio a fin).

---

## Resumen

Se rediseñó el sistema partiendo de un MVP basado en ScriptableObjects (enlace de nodos por índices
enteros, que no escalaba). Ahora el contenido se escribe en un **lenguaje de guion propio `.mcc`**
(un archivo por conversación) y la lógica lo ejecuta con un runner de pasos.

Trabajo dividido en dos tandas. **Tanda 1 = núcleo jugable (hecha).** Tanda 2 = pulido (pendiente).

---

## Tanda 1 — HECHA ✅

- [x] **Formato `.mcc`** (cabecera de tipo + condiciones, líneas con expresión, elecciones con bloque, flags).
- [x] **Parser** con errores claros `archivo:línea` (`Assets/Scripts/Dialogue/Parsing/MccParser.cs`).
- [x] **Importador** de `.mcc` que valida al guardar (`Assets/Editor/Dialogue/MccImporter.cs`).
- [x] **Elecciones que reconvergen**: cada opción abre una mini-escena multi-línea y vuelve al tronco común.
- [x] **Selección Story > Context > Idle** por NPC (`ConversationSelector`), con "una sola vez" para Story/Context.
- [x] **Expresiones por personaje** (`CharacterDef`), sin lista global; expresión inexistente = error.
- [x] **Retratos** NPC izquierda / jugador derecha (básico, con atenuación del que no habla).
- [x] **Typewriter** saltable + **TextMeshPro** (soporta rich-text `<color>`, `<b>`).
- [x] **Input formalizado** (`Assets/Input/DialogueControls.inputactions`): teclado + mando.
- [x] **Contenido de ejemplo**: `camarero_intro`, `camarero_abraham`, `camarero_idle` + personajes Camarero y Prota
      (ampliado con un ejemplo de encadenado Camarero + 2 NPCs de prueba, ver `TestSceneBuilder.cs`).
- [x] **Contadores numéricos** (flags con valor entero, no solo on/off): `add contador [N]` / `reset contador`
      en el cuerpo, `required: contador >= 3` (y `>`, `<=`, `<`, `==`, `!=`) en la cabecera. Viven en
      `GameFlags` junto a los flags booleanos, mismo ciclo de vida. Pensado para patrones
      "a la N-ésima vez, un mensaje distinto" — ver ejemplo en [README_USO.md](README_USO.md).
- [x] **Persistencia en disco (guardado/carga)**: `SaveSystem` (`Assets/Scripts/Core/`) vuelca `GameFlags`
      (booleanas + contadores), `ConversationHistory` y **la posición del jugador** a un único archivo JSON en
      `Application.persistentDataPath` (`mcclarens_save.json`, una sola ranura). **Autoguardado** tras cada
      conversación terminada (`DialogueRunner.EndConversation`) y tras interacciones que cambian flags
      (`SimpleInteractable`). La posición la aporta/restaura `PlayerPersistence` (componente en el jugador).
- [x] **Menú principal** (`MainMenu.unity`, generado por `Assets/Editor/MainMenuBuilder.cs`): "Jugar" detecta
      si hay partida → "Nueva partida" (sin save), "Continuar"/"Borrar" (save en curso), o solo "Borrar" con
      aviso de terminada (save completado, no continuable). Lógica en `Assets/Scripts/UI/MainMenuController.cs`.
      El builder deja MainMenu como escena 0 en Build Settings.
- [x] **Final del juego**: al completar los maestros el Camarero da la llave (`set has_key`); la puerta
      (`ExitDoorInteractable`, en la pared sur) termina el juego al salir con la llave: marca el flag
      `game_completed`, guarda y vuelve al menú. Esa partida ya no se puede continuar.

## Tanda 2 — PENDIENTE ⬜

- [ ] Tags de **efecto** de texto: `<shake>`, `<wave>`, pausas, cambios de velocidad en línea.
- [ ] **Comandos de cinemática** embebidos: `[wait 0.5]`, `[move Abraham stage]`, `[shake box]`, Timeline.
      (El parser ya los reconoce como `CommandStep`; el runner los ignora con un aviso).
- [ ] **Estilos** de diálogo: narrador, pensamiento, sueño, sin-retrato.
- [ ] **Sonido de texto** (por personaje o global; aún sin decidir).

---

## Mapa de archivos

| Archivo | Rol |
|---|---|
| `Assets/Scripts/Dialogue/Parsing/MccParser.cs` | Texto `.mcc` → pasos tipados. Errores con línea. |
| `Assets/Scripts/Dialogue/Model/DialogueStep.cs` | `LineStep`, `ChoiceStep`+`ChoiceOption`, `FlagStep`, `CounterStep`, `CommandStep`. |
| `Assets/Scripts/Dialogue/Model/ConversationType.cs` | Enum Story/Context/Idle. |
| `Assets/Scripts/Dialogue/Model/ConversationCondition.cs` | Condiciones de flags (`required` / `!`) y de contadores (`CounterCondition`, comparadores). |
| `Assets/Scripts/Dialogue/Model/ConversationAsset.cs` | Asset generado desde un `.mcc` (tipo + condición + texto). |
| `Assets/Editor/Dialogue/MccImporter.cs` | ScriptedImporter de `.mcc` (valida personajes/expresiones). |
| `Assets/Scripts/Dialogue/CharacterDef.cs` | Personaje: nombre, lado, expresiones (con retratos). |
| `Assets/Scripts/Dialogue/CharacterRegistry.cs` + `CharacterRegistryLoader.cs` | Registro nombre→personaje en runtime. |
| `Assets/Scripts/Dialogue/ConversationSelector.cs` | Elige conversación por prioridad. |
| `Assets/Scripts/Dialogue/ConversationHistory.cs` | Marca conversaciones ya jugadas (en memoria). |
| `Assets/Scripts/Dialogue/DialogueRunner.cs` | Ejecuta la conversación (pila de pasos, input, flags). |
| `Assets/Scripts/Dialogue/DialogueUI.cs` | Presentación: TMP, typewriter, retratos. |
| `Assets/Scripts/Interaction/NpcInteractable.cs` | Un NPC + su lista de conversaciones. |
| `Assets/Input/DialogueControls.inputactions` | Acciones de input del diálogo. |
| `Assets/Scripts/Core/GameFlags.cs` | Flags booleanas + contadores enteros del mundo (con snapshot para guardado). |
| `Assets/Scripts/Core/SaveData.cs` + `SaveSystem.cs` | Estado serializable a JSON (flags, contadores, historial, posición, completado) y guardado/carga/borrado (una ranura). |
| `Assets/Scripts/Core/PlayerPersistence.cs` | Aporta/restaura la posición del jugador en el guardado. |
| `Assets/Scripts/Interaction/ExitDoorInteractable.cs` | Puerta de salida: termina el juego si tienes la llave. |
| `Assets/Scripts/UI/MainMenuController.cs` | Lógica del menú (jugar → crear/continuar/borrar/terminada). |
| `Assets/Editor/MainMenuBuilder.cs` | Genera `MainMenu.unity` y configura Build Settings. |
| `Assets/Editor/TestSceneBuilder.cs` | Genera la escena de juego/prueba y cablea todo. |

---

## Pendiente de verificar (cuando haya Unity)

No hay Unity CLI en el equipo de desarrollo, así que cada cambio se prueba abriendo el Editor a mano.
Pasos de prueba en [README_USO.md](README_USO.md#probar-el-sistema). Los contadores numéricos (recién
añadidos) todavía no se han probado en el Editor — revisar la Consola tras el primer build con ellos.
