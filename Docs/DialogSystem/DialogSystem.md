# Sistema de Diálogos — Visión y decisiones

> **Qué es este documento.** Recoge la **filosofía** y las **decisiones de diseño** del sistema de
> diálogos: cómo debe *sentirse*, qué debe hacer y qué problemas resuelve. **No** describe la
> implementación técnica.
>
> **Documentos hermanos:**
> - [ARQUITECTURA.md](ARQUITECTURA.md) — cómo está construido por dentro (clases, flujo, extensión).
> - [ESTADO.md](ESTADO.md) — qué está hecho y qué falta (Tanda 1 / Tanda 2).
> - [README_USO.md](README_USO.md) — manual práctico para escribir conversaciones en `.mcc`.

---

## 1. Por qué el sistema de diálogos es el más importante

McClarens es un juego narrativo en 2D donde **el elemento central son las conversaciones** con los
personajes. No es un RPG clásico ni una aventura gráfica clásica; los minijuegos son contenido secundario.

Lo que de verdad importa en el juego es:

- descubrir la historia,
- conocer a los personajes,
- hablar con ellos,
- crear momentos divertidos.

Por eso el sistema de diálogos es **el sistema más importante del proyecto** y donde se invertirá la mayor
parte del tiempo de desarrollo.

## 2. Filosofía

El objetivo no es "mostrar texto". El objetivo es que **escribir una conversación sea rápido, intuitivo y
divertido**. Como la mayor parte del trabajo será escribir diálogos, crear una conversación debe requerir
el mínimo esfuerzo posible.

> **Regla guía:** escribir un diálogo debe sentirse más parecido a **escribir un guion** que a programar.

### Reutilización

Todo el sistema debe ser **reutilizable**. Un único sistema soporta todas las conversaciones del juego:

- no se crea un sistema distinto por personaje,
- no se escribe código nuevo por conversación.

### Contenido antes que tecnología

Da igual el formato de almacenamiento subyacente (JSON, ScriptableObjects, YAML, un lenguaje propio…).
Lo que importa es que **escribir conversaciones sea cómodo**. → *Decisión tomada: lenguaje propio `.mcc`;
ver [ARQUITECTURA.md](ARQUITECTURA.md).*

### Escalabilidad

El juego tendrá **cientos de conversaciones**. Añadir una nueva debe ser una tarea sencilla y **no** obligar
a tocar el código.

---

## 3. Organización de las conversaciones

**Un archivo por conversación**, no un archivo gigante por personaje. Ejemplos:

```
camarero_intro
camarero_tv_rota
camarero_capitulo_2
camarero_fin
```

Esto facilita mantenimiento, búsqueda, depuración y ampliación del juego.

---

## 4. Tipos de conversación

Existen tres grandes tipos, con un **orden de prioridad** al hablar con un NPC:

| Tipo | Cuándo | Frecuencia | Ejemplo |
|---|---|---|---|
| **Story** | Hacen avanzar la historia | Una sola vez | El camarero se presenta. |
| **Context** | Reaccionan a un evento | Una sola vez | El jugador descubre la tele rota y el camarero lo comenta. |
| **Idle** | Relleno por defecto | Repetible | "¿Qué tal?", "Estoy limpiando vasos." |

**Regla de selección** — cuando el jugador habla con un NPC:

1. ¿Hay alguna **Story** disponible? → se reproduce.
2. Si no, ¿hay alguna **Context**? → se reproduce.
3. Si no, se reproduce una **Idle**.

### Conversaciones opcionales (dar vida al mundo)

El mundo reacciona a lo que hace el jugador: descubrir una tele rota, encontrar un objeto, entrar en una
habitación, hablar con otro personaje… Los NPC pueden tener pequeños comentarios sobre esos eventos.
Son **totalmente opcionales** y solo sirven para que el mundo se sienta vivo.

---

## 5. Elecciones

Las elecciones **NO cambian la historia. Nunca.** Su único objetivo es **aumentar la inmersión** y el humor.

- El botón muestra un **estado de ánimo / actitud** corto (`Bien`, `Regular`, `Chulo`, `¿Eres tonto?`),
  **no** el texto literal que dirá el protagonista.
- Al elegirlo, el protagonista suelta una respuesta **más larga** (que se escribe aparte). Puede ser incluso
  un pequeño intercambio de varias líneas donde el NPC responda de forma ligeramente distinta.
- Después, **todas las ramas vuelven al mismo punto** de la conversación (reconvergen).

```
NPC:  ¿Qué tal?
        → Bien      → (respuesta larga A)  ┐
        → Regular   → (respuesta larga B)  ├─→ vuelven al mismo punto
        → Mal       → (respuesta larga C)  ┘
```

No existen árboles narrativos complejos.

---

## 6. Presentación

### Retratos

Durante los diálogos se muestran retratos. Por defecto: **NPC a la izquierda, jugador a la derecha.**
El mismo motor debe soportar distintos estilos:

- diálogo normal,
- narrador,
- pensamiento,
- sueño,
- conversación sin retratos.

### Expresiones

Cada personaje tiene **sus propias expresiones**; **no** hay una lista global.

```
Braham:    normal, feliz, loco, sorprendido
Profesor:  normal, enfadado
```

Las expresiones irán creciendo durante el desarrollo. Si un diálogo usa una expresión inexistente, se
quiere un **error claro** — el sistema **no** debe sustituir una expresión por otra automáticamente.

### Texto

- Todo el texto aparece **escribiéndose poco a poco** (typewriter).
- Se puede **acelerar**.
- Sonido del texto (por personaje o único global): **sin decidir aún**.

### Formato enriquecido

El texto soporta etiquetas para remarcar momentos importantes: `shake`, `wave`, `color`, negrita, pausas,
velocidad.

```
<shake>¡¡NO!!</shake>
<color=red>PELIGRO</color>
```

### Efectos

El sistema debe poder soportar efectos visuales: vibrar el texto / el retrato / la caja, cambiar de
expresión, cambiar la velocidad del texto… No es prioritario implementarlos todos desde el principio.

---

## 7. El jugador y el mundo durante un diálogo

- Durante un diálogo, **el jugador no puede moverse**.
- Sin embargo, **el mundo puede seguir funcionando**.
- Se quiere poder hacer **cinemáticas** antes, durante o después del diálogo:

```
Empieza un diálogo → un personaje camina → continúa el diálogo
```

---

## 8. Qué se espera del desarrollador y de Claude

**Del desarrollador:** define únicamente el *comportamiento esperado* (este documento). La estructura de
clases, la organización del código, el formato de almacenamiento y la arquitectura interna quedan a su
criterio, siempre respetando esta visión.

**De Claude (arquitecto del sistema):** no se trata solo de escribir código. Se espera que:

- proponga una arquitectura y **justifique** sus decisiones,
- **detecte** posibles problemas y proponga mejoras,
- mantenga el sistema **escalable**,
- **explique** antes de implementar si cree que algo puede mejorarse.

El objetivo es un sistema **sólido y mantenible**, no simplemente funcional.

---

## 9. Estado de las decisiones

Las decisiones concretas ya tomadas sobre esta visión (formato `.mcc`, prioridad de selección, retratos con
TextMeshPro, input, etc.) y su grado de implementación están en **[ESTADO.md](ESTADO.md)** y
**[ARQUITECTURA.md](ARQUITECTURA.md)**.
