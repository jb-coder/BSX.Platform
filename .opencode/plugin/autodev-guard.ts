import { appendFileSync, existsSync, statSync } from "node:fs"
import { join } from "node:path"
import type { Plugin } from "@opencode-ai/plugin"

const LOCK_FILE = ".opencode/state/autodev.lock"
const STOP_FILE = ".opencode/state/STOP"
const LOG_FILE = ".opencode/state/autodev.log"
const LOCK_TTL_MS = 4 * 60 * 60 * 1000

type Rule = {
  pattern: RegExp
  reason: string
}

const DANGEROUS_BASH: Rule[] = [
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+push\b/i,
    reason: "git push está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+reset\s+--hard\b/i,
    reason: "git reset --hard está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+clean\b/i,
    reason: "git clean está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+rebase\b/i,
    reason: "git rebase está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+filter-branch\b/i,
    reason: "git filter-branch está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+update-ref\b/i,
    reason: "git update-ref está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*git(?:\.exe)?\s+branch\s+-D\b/i,
    reason: "Borrar ramas está prohibido durante autodev",
  },
  {
    pattern: /Remove-Item\b[^\n]*-Recurse/i,
    reason: "Remove-Item -Recurse está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*(rm|del)\s+[^\n]*(-rf|\/s)\b/i,
    reason: "Borrado recursivo está prohibido durante autodev",
  },
  {
    pattern: /\bFormat-Volume\b/i,
    reason: "Formatear volúmenes está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*(pwsh|powershell)(?:\.exe)?\s+/i,
    reason: "Abrir una shell anidada está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*cmd(?:\.exe)?\s+/i,
    reason: "Abrir cmd está prohibido durante autodev",
  },
  {
    pattern: /(^|[;&|])\s*(iex|Invoke-Expression)\b/i,
    reason: "Invoke-Expression está prohibido durante autodev",
  },
]

const STOP_SAFE_BASH: RegExp[] = [
  /^\s*Test-Path\b/i,
  /^\s*Get-Content\b/i,
  /^\s*Get-Date\b/i,
  /^\s*Get-ChildItem\b/i,
  /^\s*Add-Content\b/i,
  /^\s*Remove-Item\b[^\n]*autodev\.lock/i,
  /^\s*git(?:\s+|\.exe\s+)(status|diff|log|rev-parse|show|branch)\b/i,
  /^\s*git(?:\s+|\.exe\s+)checkout\s+develop\b/i,
  /^\s*git(?:\s+|\.exe\s+)switch\s+develop\b/i,
]

const SENSITIVE_EDIT: Rule[] = [
  {
    pattern: /(^|\/)docs\/decisions\//i,
    reason: "Los ADRs requieren aprobación humana",
  },
  {
    pattern: /(^|\/)docs\/security\//i,
    reason: "La documentación de seguridad requiere aprobación humana",
  },
  {
    pattern: /(^|\/)docs\/operations\/security-checklist\.md$/i,
    reason: "El checklist de seguridad requiere aprobación humana",
  },
  {
    pattern: /(^|\/)Migrations\//i,
    reason: "Las migraciones requieren aprobación humana",
  },
  {
    pattern: /(^|\/)\.github\//i,
    reason: "La configuración de CI requiere aprobación humana",
  },
  {
    pattern: /(^|\/)\.opencode\//i,
    reason: "La configuración de opencode requiere aprobación humana",
  },
  {
    pattern: /(^|\/)Directory\.(Build|Packages)\.props$/i,
    reason: "La configuración central del build requiere aprobación humana",
  },
  {
    pattern: /(^|\/)global\.json$/i,
    reason: "La versión del SDK requiere aprobación humana",
  },
  {
    pattern: /\.csproj$/i,
    reason: "Los archivos de proyecto requieren aprobación humana",
  },
  {
    pattern: /\.(props|targets)$/i,
    reason: "Los archivos MSBuild requieren aprobación humana",
  },
  {
    pattern: /appsettings(\..+)?\.json$/i,
    reason: "La configuración de la aplicación requiere aprobación humana",
  },
  {
    pattern: /launchSettings\.json$/i,
    reason: "La configuración de arranque requiere aprobación humana",
  },
  {
    pattern: /(^|\/)\.env/i,
    reason: "Los archivos de entorno requieren aprobación humana",
  },
  {
    pattern: /secrets/i,
    reason: "Los secretos requieren aprobación humana",
  },
  {
    pattern: /\.(pem|key)$/i,
    reason: "Las claves requieren aprobación humana",
  },
]

function isAutodevActive(directory: string): boolean {
  const lockPath = join(directory, LOCK_FILE)

  if (!existsSync(lockPath)) {
    return false
  }

  try {
    const age = Date.now() - statSync(lockPath).mtimeMs
    return age <= LOCK_TTL_MS
  } catch {
    return false
  }
}

function log(directory: string, message: string): void {
  try {
    appendFileSync(
      join(directory, LOG_FILE),
      `${new Date().toISOString()} | guard | ${message}\n`,
      "utf8",
    )
  } catch {
    // Logging must never break the tool call.
  }
}

function isAllowedStateFile(filePath: string): boolean {
  return /(^|\/)\.opencode\/state\/backlog\.json$/i.test(filePath)
}

function guardBash(directory: string, command: string, stopActive: boolean): void {
  const trimmed = command.trim()

  if (!trimmed) {
    return
  }

  const dangerous = DANGEROUS_BASH.find((rule) => rule.pattern.test(trimmed))

  if (dangerous) {
    log(directory, `blocked dangerous bash: ${trimmed}`)
    throw new Error(`autodev-guard: ${dangerous.reason}. Comando bloqueado: ${trimmed}`)
  }

  if (!stopActive) {
    return
  }

  const allowsLockCleanup = trimmed.includes("autodev.lock")
  const safe = STOP_SAFE_BASH.some((pattern) => pattern.test(trimmed))

  if (!allowsLockCleanup && !safe) {
    log(directory, `blocked bash after STOP: ${trimmed}`)
    throw new Error(
      "autodev-guard: STOP existe. El ciclo autodev debe detenerse, registrar el estado y limpiar el lock.",
    )
  }
}

function guardEdit(directory: string, filePath: string): void {
  if (!filePath) {
    return
  }

  const normalized = filePath.replace(/\\/g, "/")

  if (isAllowedStateFile(normalized)) {
    return
  }

  const sensitive = SENSITIVE_EDIT.find((rule) => rule.pattern.test(normalized))

  if (sensitive) {
    log(directory, `blocked sensitive edit: ${filePath}`)
    throw new Error(
      `autodev-guard: edición bloqueada en ruta sensible durante autodev (${sensitive.reason}): ${filePath}`,
    )
  }
}

export default (async ({ directory }) => {
  return {
    "tool.execute.before": async (input, output) => {
      if (!isAutodevActive(directory)) {
        return
      }

      const stopActive = existsSync(join(directory, STOP_FILE))

      if (input.tool === "bash") {
        const command = String((output.args as { command?: unknown } | undefined)?.command ?? "")
        guardBash(directory, command, stopActive)
        return
      }

      if (input.tool === "edit" || input.tool === "write") {
        const args = output.args as { filePath?: unknown; path?: unknown } | undefined
        const filePath = String(args?.filePath ?? args?.path ?? "")
        guardEdit(directory, filePath)
      }
    },
  }
}) satisfies Plugin
