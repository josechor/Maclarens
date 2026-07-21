# Cómo escribir diálogos (formato `.mcc`)

Guía práctica para crear conversaciones en McClarens. Para el estado del sistema, ver [ESTADO.md](ESTADO.md).
Para la filosofía, [DialogSystem.md](DialogSystem.md).

La idea: **escribir una conversación se parece a escribir un guion**. Cada conversación es un archivo de
texto `.mcc` dentro de `Assets/Dialogue/`. Al guardarlo, Unity lo valida solo y te avisa de errores en la
Consola.

---

## 1. Un ejemplo completo, comentado

```
# Los comentarios empiezan con # y se ignoran. Las líneas en blanco también.

# --- CABECERA (obligatoria, primera línea) ---
# Tipo de conversación + cuándo está disponible.
@story   required: !intro_done

# --- LÍNEAS DE DIÁLOGO ---
# Formato:  Personaje [expresion]: texto
Camarero [normal]: Vaya, despertaste... por fin.
Camarero [feliz]: <color=#ffcc00>¡Estás encerrado, majo!</color>

# --- UNA ELECCIÓN ---
# El botón muestra el ÁNIMO (corto). Al elegirlo se ejecuta su bloque indentado
# (pueden hablar varios personajes) y luego la conversación SIGUE en el tronco común.
? Prota:
  - Chulo:
      Prota [normal]: Tú tranquilo. He salido de sitios peores que este antro.
      Camarero [feliz]: Ja. Ese es el espíritu.
  - Nervioso:
      Prota [normal]: ¿C-cómo que encerrado? ¡Yo solo venía a por una caña!
      Camarero [normal]: Respira, hombre. Hay salida.
  - ¿Eres tonto?:
      Camarero [enfadado]: Oye, un respeto, que llevo aquí toda la noche.
      Prota [normal]: Vale, vale... perdón.

# Reconvergencia: pase lo que pase arriba, la historia continúa igual desde aquí.
Camarero [normal]: Total. Habla con los Maestros y te abro la puerta.

# --- FLAGS ---
set intro_done
```

---

## 2. Reglas del formato

### Cabecera (primera línea, obligatoria)

```
@story
@context   required: TalkedToAbraham
@idle
@story     required: intro_done, !hablo_con_roi
```

- **`@story`** — conversación principal. Ocurre **una vez**. Máxima prioridad.
- **`@context`** — comentario sobre un evento. Ocurre **una vez**. Prioridad media.
- **`@idle`** — relleno por defecto. **Repetible**. Prioridad baja.
- `required:` (opcional) — lista de flags separadas por comas:
  - `flag` → esa flag debe estar **activa**.
  - `!flag` → esa flag debe estar **sin activar**.

Cuando el jugador habla con un NPC, el sistema elige **la conversación disponible de mayor prioridad**
(Story > Context > Idle). Las de "una vez" ya jugadas dejan de aparecer.

### Línea de diálogo

```
Personaje [expresion]: texto
Personaje: texto
```

- `Personaje` debe coincidir con el nombre de un `CharacterDef` (ver sección 3).
- `[expresion]` es opcional; si la omites, se usa la primera expresión del personaje.
- El texto admite **rich-text de TextMeshPro**: `<color=#ff0000>rojo</color>`, `<b>negrita</b>`.
  (Los tags de efecto como `<shake>` llegarán en la Tanda 2.)

### Elección

```
? Prota:
  - EtiquetaÁnimo:
      <una o más líneas>
  - OtroÁnimo:
      <una o más líneas>
```

- El texto del botón es la **etiqueta de ánimo** (corta): `Chulo`, `Nervioso`, `¿Eres tonto?`…
- Debajo de cada opción, **indentado**, va su bloque de líneas (pueden hablar el prota y el NPC).
- Al acabar el bloque, la conversación **vuelve al tronco común**. Las elecciones **nunca cambian la
  historia**, solo el humor.
- La indentación es con **espacios** (no tabuladores). Las opciones van más indentadas que el `?`, y las
  líneas de cada opción más indentadas que su `-`.

### Flags

```
set nombre_flag
unset nombre_flag
```

Activan o desactivan una flag global en ese punto exacto de la conversación. Sirven para condicionar
otras conversaciones (con `required:` en su cabecera).

### Comandos (Tanda 2, todavía no hacen nada)

```
[wait 0.5]
[move Abraham stage]
```

El parser ya los reconoce, pero de momento se ignoran con un aviso. Se implementarán en la Tanda 2.

---

## 3. Personajes y expresiones

Cada personaje es un **CharacterDef** (un asset). Define su nombre, su lado en pantalla y **sus propias
expresiones** (no hay lista global).

**Crear uno a mano:** clic derecho en `Assets/Dialogue/Characters/` → `Create ▸ McClarens ▸ Character`.
Rellena:
- **Character Name**: el nombre exacto que usarás en los `.mcc` (ej. `Camarero`).
- **Default Side**: `Left` para NPCs, `Right` para el jugador.
- **Expressions**: lista de `{ id, portrait }`. El `id` es lo que pones entre `[ ]` (ej. `normal`, `feliz`).

> ⚠️ Si un `.mcc` usa una expresión que el personaje **no** tiene, salta un **error** al guardar (no se
> sustituye por otra). Es a propósito: así detectas erratas al instante.

Para que el juego los conozca en runtime, deben estar en la lista `Characters` del **CharacterRegistryLoader**
(está en el objeto `DialogueRunner` de la escena). El `TestSceneBuilder` ya mete a Camarero y Prota.

---

## 4. Añadir una conversación nueva

1. Crea un archivo `Assets/Dialogue/mi_conversacion.mcc` y escríbela (formato de arriba).
2. Guarda. Mira la **Consola** de Unity: si hay algún error, sale como `mi_conversacion.mcc:línea — …`.
3. Asigna la conversación al NPC: selecciónalo en la escena y arrastra el asset `.mcc` a la lista
   **Conversations** de su componente `NpcInteractable`.
   *(En la escena de prueba, el `TestSceneBuilder` lo hace por código para el Camarero.)*

Convención de nombres: `personaje_tema` → `camarero_intro`, `roi_tv_rota`, `daniel_fin`…

---

## 5. Controles durante el diálogo

| Acción | Teclado | Mando |
|---|---|---|
| Avanzar / confirmar / acelerar texto | `E`, `Enter`, o clic | Botón A (South) |
| Moverse entre opciones | `W`/`S` o flechas ↑/↓ | Cruceta ↑/↓ |
| Elegir opción directa | `1`–`4` | — |

Primera pulsación de avanzar mientras el texto se escribe → lo **completa de golpe**. Segunda → **avanza**.

Se configuran en `Assets/Input/DialogueControls.inputactions` (puedes añadir más bindings ahí).

---

## 6. Probar el sistema

> Requiere Unity abierto (no hay CLI en el equipo de desarrollo).

1. **Una vez**: `Window ▸ TextMeshPro ▸ Import TMP Essential Resources`.
2. Menú **`McClarens ▸ Build Test Room Scene`** → genera `Assets/Scenes/TestRoom.unity`.
3. Abre esa escena y dale a **Play**.
4. Camina hasta el **Camarero** y pulsa `E`:
   - Sale la intro (`@story`) con typewriter, retratos y elecciones con bloque.
   - Elige un ánimo → se reproduce su mini-escena → reconverge.
   - Al terminar se activa `intro_done`.
5. Vuelve a hablarle → ahora sale la **idle** (la story ya ocurrió).
6. Habla antes con **Abraham** (activa `TalkedToAbraham`) y luego con el Camarero → aparece la **context**.
7. **Probar errores**: edita `camarero_intro.mcc`, pon `[expresion_que_no_existe]`, guarda y mira la Consola.

---

## 7. Referencia rápida

```
@story | @context | @idle          cabecera (1ª línea)
required: flagA, !flagB            condición opcional en la cabecera
Personaje [expr]: texto            línea hablada
? Prota:                           inicio de elección
  - Ánimo:                         opción (botón)
      Personaje: texto             bloque de la opción (indentado)
set flag  /  unset flag            activar/desactivar flag
[comando args]                     comando de cinemática (Tanda 2)
# comentario                       ignorado
<color=#hex>...</color>, <b>...</b>  rich-text de TMP
```
