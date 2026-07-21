# Sistema de Diálogos — Arquitectura técnica

> Referencia interna: **cómo está construido** el sistema por dentro. Para la visión, ver
> [DialogSystem.md](DialogSystem.md); para el estado, [ESTADO.md](ESTADO.md); para escribir contenido,
> [README_USO.md](README_USO.md).

---

## 1. Principio rector: contenido separado de lógica

El sistema respeta el principio del proyecto *"los diálogos deben estar separados de la lógica"*
(ver [DevPrinciples.md](../Technical/DevPrinciples.md)). Se organiza en cuatro capas:

```
CONTENIDO            LÓGICA                     PRESENTACIÓN
─────────            ──────                     ────────────
.mcc  (texto)   →    MccParser  →  pasos   →    DialogueRunner  →  DialogueUI (TMP)
CharacterDef (SO)                              (pila de pasos)     retratos / typewriter
```

- **Contenido**: archivos `.mcc` (texto, un archivo por conversación) + `CharacterDef` (ScriptableObjects,
  porque referencian sprites de retrato).
- **Modelo**: pasos de diálogo tipados.
- **Lógica**: parser + selector + runner.
- **Presentación**: `DialogueUI`.

---

## 2. Flujo de una conversación

```
Jugador pulsa E frente a un NPC
        │
        ▼
NpcInteractable.Interact()
        │   pide al selector la conversación de mayor prioridad disponible
        ▼
ConversationSelector.Select(conversations)      Story > Context > Idle
        │   (filtra por condiciones de flags y "ya jugada")
        ▼
DialogueRunner.StartConversation(convo, player)
        │   convo.ParseSteps()  → List<DialogueStep>
        │   bloquea al jugador, habilita el input de diálogo
        ▼
DialogueRunner  (recorre los pasos con una PILA de frames)
        │
        ├─ LineStep   → DialogueUI.ShowLine(...)   (espera "avanzar")
        ├─ ChoiceStep → DialogueUI.ShowChoices(...) (espera selección)
        ├─ FlagStep   → GameFlags.Set/Clear         (sin UI, sigue)
        └─ CommandStep→ (Tanda 2: cinemáticas)      (sin UI, sigue)
        ▼
Fin → ConversationHistory.MarkPlayed(id) si es "una vez"
      desbloquea al jugador
```

### La pila de pasos (reconvergencia)

El runner mantиene una `Stack<Frame>`, donde `Frame = { List<DialogueStep> pasos, int índice }`.

- Al empezar, se apila el frame raíz (los pasos de la conversación).
- Al llegar a un `ChoiceStep`, el índice del frame **ya ha avanzado** más allá de la elección. Cuando el
  jugador elige una opción, se apila un frame nuevo con el **bloque** de esa opción.
- Cuando ese bloque se agota, se **desapila** y se continúa en el frame padre, justo **después** de la
  elección → **reconvergencia automática**, sin índices que mantener a mano.

Esta es la mejora clave frente al MVP anterior (que enlazaba nodos por índices enteros y no escalaba).

---

## 3. El formato `.mcc` y su parseo

`MccParser.Parse(texto)` → `ParsedConversation { Type, Condition, Steps }`.

- **Cabecera**: `@story|@context|@idle` + `required: flagA, !flagB`.
- **Cuerpo**: se parsea con descenso recursivo guiado por **indentación** (solo espacios):
  - `? Personaje:` abre un `ChoiceStep`; sus opciones (`- Etiqueta:`) van más indentadas;
  - el **bloque** de cada opción va aún más indentado y se parsea recursivamente (puede contener
    líneas, flags e incluso elecciones anidadas);
  - una línea con menor indentación cierra el bloque.
- **Errores**: `MccParseException(línea, mensaje)`. El importador los formatea como `archivo:línea — mensaje`.

### Pasos (`DialogueStep`)

| Paso | Contenido |
|---|---|
| `LineStep` | `Speaker`, `Expression` (opcional), `Text` |
| `ChoiceStep` | `Speaker`, `List<ChoiceOption>` (cada una: `MoodLabel` + `Body`) |
| `FlagStep` | `Flag`, `Value` (set/unset) |
| `CommandStep` | `Name`, `Args[]` — reservado para Tanda 2 |

Todos llevan `Line` (nº de línea de origen) para errores claros.

---

## 4. Importación (edit-time)

`MccImporter` es un `ScriptedImporter` para la extensión `.mcc`:

1. Parsea el archivo (errores de sintaxis a la Consola al guardar).
2. **Valida** personajes y expresiones contra los `CharacterDef` del proyecto (vía `AssetDatabase`).
   Personaje o expresión inexistente = error de importación; **no** hay sustitución automática.
3. Produce un `ConversationAsset` (tipo + condición + texto fuente) como asset principal del `.mcc`.

El `ConversationAsset` guarda solo la **cabecera parseada** (barata de consultar por el selector) y el
**texto**; el cuerpo se **reparsea a pasos** al iniciar la conversación (coste despreciable; evita
serializar jerarquías polimórficas de pasos).

`ctx.DependsOnSourceAsset` sobre cada `CharacterDef` hace que editar un personaje reimporte las
conversaciones y revalide.

---

## 5. Personajes

`CharacterDef` (ScriptableObject): `characterName`, `defaultSide` (Left/Right) y una lista de
`Expression { id, portrait }` — **cada personaje tiene su propio set**, sin lista global.

En runtime, `CharacterRegistry` (estático) resuelve nombre → `CharacterDef`. Lo rellena
`CharacterRegistryLoader` (componente en la escena) al arrancar.

---

## 6. Selección y persistencia

- `ConversationSelector.Select(...)`: de las conversaciones de un NPC, descarta las no disponibles
  (condición de flags no cumplida, o "una vez" ya jugada) y devuelve la de **mayor prioridad**
  (Story 3 > Context 2 > Idle 1; a igual prioridad, la primera de la lista).
- `ConversationAsset.IsOnce`: Story y Context son de una vez; Idle repetible.
- `ConversationHistory`: marca conversaciones jugadas. **Tanda 1: solo en memoria** (se reinicia cada
  partida). La persistencia en disco es Tanda 2.
- `GameFlags` (`Assets/Scripts/Core/GameFlags.cs`): estado global de flags, también en memoria por ahora.

---

## 7. Presentación e input

- `DialogueUI` (TextMeshPro): typewriter con `maxVisibleCharacters` (saltable con `CompleteTyping()`),
  retratos izquierda/derecha según `CharacterDef.defaultSide` (el que no habla se atenúa). Soporta
  rich-text de TMP (`<color>`, `<b>`).
- Input: `Assets/Input/DialogueControls.inputactions` (mapa `Dialogue`: `Advance`, `NavigateUp/Down`,
  `Choice1..4`; teclado + mando). El `DialogueRunner` habilita el mapa al empezar y lo deshabilita al
  terminar. El ratón usa los `Button` de uGUI (opción y "continuar").

---

## 8. Puntos de extensión para la Tanda 2

| Objetivo | Dónde engancharlo |
|---|---|
| Tags de efecto (`<shake>`, `<wave>`, pausas) | En `DialogueUI` al revelar el texto (parsear tags propios antes de pasarlos a TMP). |
| Comandos de cinemática (`[wait]`, `[move]`) | `DialogueRunner.ShowNext()` ya recibe `CommandStep`; hoy lo ignora. Ahí se despacharía a un `CommandExecutor`. |
| Mundo vivo durante el diálogo | El jugador ya se bloquea sin congelar la escena; integrar con `Timeline` (ya en los paquetes). |
| Estilos (narrador, pensamiento, sueño, sin-retrato) | Añadir un modificador de estilo al `LineStep`/cabecera y variantes de layout en `DialogueUI`. |
| Persistencia | Serializar `GameFlags` + `ConversationHistory` a disco (save system). |

---

## 9. Mapa de archivos

Ver la tabla completa en [ESTADO.md](ESTADO.md#mapa-de-archivos).
