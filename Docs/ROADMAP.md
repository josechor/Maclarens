# Roadmap de producción — McClarens

> Ruta desde el sistema de diálogos hasta el juego terminado.
> Ordenado por **dependencias** y por **riesgo**, no por lo que apetece hacer.
> Regla que atraviesa todo: *terminar el juego antes que añadir sistemas complejos* (DevPrinciples nº1).

Última actualización: **2026-07-21**.

---

## Idea rectora del orden

En un juego narrativo de humor, **el riesgo está en la escritura y el ritmo, no en la estética**.
Por eso el orden es: *probar que mola → construir el juego entero feo → embellecer al final*.

La estética real es lo **último**. La única "estética" que va temprano son los **estilos de diálogo**
(narrador / pensamiento / sueño), porque cambian cómo *escribes*, no cómo se *ve*.

```
FASE 0  Verificar base        ──┐  (desbloquea todo)
FASE 1  Vertical slice        ──┤  ¿es divertido? ¿la regla de oro funciona?
FASE 2  Guardado + Debug      ──┤  (desbloquea producir contenido rápido)
FASE 3  Juego completo (feo)  ──┤  ¿aguanta 1h de humor y puzzles?
FASE 4  El bar vivo           ──┤  detalles, secretos, logros
FASE 5  Estética              ──┤  arte final
FASE 6  Pulido y cierre       ──┘  playtest, menús finales, build
```

---

## FASE 0 — Verificar la base ⚠️ (bloqueante)

**Objetivo:** confirmar que lo que ya está escrito *arranca*. La Tanda 1 del diálogo nunca se ha
compilado en Unity (`DialogSystem/ESTADO.md`).

- Abrir el proyecto en Unity y resolver errores de compilación (típico: API de TextMeshPro).
- Jugar la escena de prueba: un diálogo completo con retratos, typewriter, elecciones.

**Hecho =** un diálogo de ejemplo se juega de principio a fin sin errores en consola.

---

## FASE 1 — Vertical slice: intro → primer maestro

**Objetivo:** NO que sea bonito. Validar que **engancha** y que la **regla de oro** funciona
(el absurdo colando como normal). Todo con placeholders.

Es la primera hora de juego comprimida en su tramo inicial: despiertas encerrado → exploras el hub →
llegas al primer maestro (los ~5 primeros minutos) → su reto → su primer minijuego.

**Entregables:**
- **Estilos de diálogo** (narrador / pensamiento / sueño / sin-retrato) — esto es estructura, va aquí
  porque afecta a cómo escribes la intro. *(Es la Tanda 2 del diálogo.)*
- **Boceto jugable del escenario principal** (hub del bar): tilemap + zonas + colisiones. Feo pero navegable.
- **Contenido del primer maestro**: sus conversaciones (`.mcc`), su reto, qué entrega.
- **Primer minijuego** como **escena/prefab autónomo** (arranca solo con Play — ver Fase 2).
- **Sistema de interacción** genérico (acercarse a un NPC/objeto y activar).

**Hecho =** puedes jugar del despertar al primer minijuego de un tirón, y da risa.

---

## FASE 2 — Guardado + Debug (el mismo sistema)

**Objetivo:** dejar de jugar desde el principio para probar cada cosa. Guardar una partida es
**serializar el estado** (`GameFlags` + `ConversationHistory` + posición + escena). El menú de debug
es solo "cargar una partida-preset". Se construyen juntos porque son la misma pieza.

**Entregables:**
- **Persistencia de estado** en disco (cierra la Tanda 2 pendiente de `ESTADO.md`).
- **Menú de guardado/carga** (esqueleto de menús).
- **Menú de debug** (solo en desarrollo): set de flags a mano, teleport, cargar escena.
- **Estados de arranque predefinidos**: "Empezar desde Maestro 1 / 3 / Final".
- **Regla de oro técnica:** cada minijuego futuro debe poder jugarse dándole a Play a su escena.

**Hecho =** puedes saltar a cualquier punto del juego en segundos y guardar/cargar.

---

## FASE 3 — Juego completo con placeholders

**Objetivo:** el juego entero jugable de principio a fin. Aquí vive el riesgo real:
**¿aguantan 1-2h de humor y puzzles?** Se responde con contenido completo, no con arte.

**Entregables:**
- Los **5 maestros** completos: conversaciones, retos, qué entrega cada uno, orden de progresión.
- Los **minijuegos** de cada uno (autónomos, probados por separado gracias a la Fase 2).
- **Progresión** completa hasta el **final del juego** (salir del McClarens).
- Todas las salas/plantas del bar, navegables (feas).

**Hecho =** alguien puede jugar del despertar al final sin tu ayuda. Feo, pero completo.

---

## FASE 4 — El bar vivo (detalles y recompensas)

**Objetivo:** DevPrinciples nº5 (el McClarens debe sentirse vivo) y nº6 (recompensar la exploración).

**Entregables:**
- **Sistema de logros** funcional + logros reales.
- **Secretos y referencias** escondidos (recompensa a explorar).
- Detalles ambientales: NPCs de relleno, televisiones, partículas, música/ambiente.
- **Sonido de texto** del diálogo (Tanda 2 pendiente).

**Hecho =** el bar se siente habitado y explorar tiene premio, aún con arte provisional.

---

## FASE 5 — Estética (arte final)

**Objetivo:** ahora sí, la belleza, sobre una base validada y completa.

**Entregables:**
- Retratos finales de los personajes (por expresión).
- Tilesets y objetos finales del escenario (cenital ligeramente inclinado, no pixel art).
- Skin final de la UI de diálogo por estilo (narrador/pensamiento/sueño se ven distintos).
- Iluminación y capas.

**Hecho =** el juego se ve como el juego que quieres enseñar.

---

## FASE 6 — Pulido y cierre

**Objetivo:** que esté listo para que otros lo jueguen.

**Entregables:**
- **Playtest** con gente real: ajustar ritmo del humor y dificultad de puzzles.
- Menús finales: principal, opciones, créditos.
- Bugfix y balanceo.
- **Build** jugable.

**Hecho =** hay un ejecutable que alguien puede instalar y terminar.

---

## Resumen de dependencias

- **Fase 0** desbloquea todo (sin base que compile, no hay nada).
- **Fase 1** valida que el juego merece hacerse antes de invertir en las 5 fases restantes.
- **Fase 2** desbloquea la velocidad de las Fases 3-6 (probar sin rejugar).
- **Fases 3→4→5→6** son secuenciales: contenido → vida → belleza → cierre.

Regla de oro de producción: **nunca adelantar una fase de belleza a una de contenido.**
