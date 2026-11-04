---
description: "Ejecuta un lote autónomo de tareas del backlog BSX: arquitectura, implementación, review, build, tests y merge."
agent: orchestrator
---

Ejecuta el ciclo autónomo de autodev descrito en tu rol.

Argumentos recibidos:

```text
$ARGUMENTS
```

Reglas de esta invocación:

- Si no hay argumentos, procesa como máximo 1 tarea (`--max 1`).
- Interpreta `--max N`, `--task <id>` y `--dry-run`.
- Aplica el flujo obligatorio completo: precondiciones, lock, selección de tarea, arquitectura, implementación, verificación, review, chequeo de rutas sensibles, merge gate, verificación post-merge y actualización del backlog.
- No pidas confirmación salvo que falte información imprescindible. Si falta, detente y reporta.
- Respeta `.opencode/state/STOP` y elimina `.opencode/state/autodev.lock` al terminar, incluso si hay error.
- Esta invocación procesa un lote acotado; no es un demonio. Al terminar, devuelve el informe con el formato de salida esperado.
