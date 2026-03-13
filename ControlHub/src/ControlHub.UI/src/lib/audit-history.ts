import { V3InvestigateResponse } from '@/services/api/audit';

const STORAGE_KEY = 'audit_history_v3';
const MAX_ENTRIES = 10;

export interface AuditHistoryEntry {
  id: string;
  savedAt: string;
  query: string;
  correlationId?: string;
  result: V3InvestigateResponse;
}

export function loadAuditHistory(): AuditHistoryEntry[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed as AuditHistoryEntry[];
  } catch {
    return [];
  }
}

export function saveAuditResult(
  query: string,
  correlationId: string | undefined,
  result: V3InvestigateResponse
): AuditHistoryEntry[] {
  const existing = loadAuditHistory();
  const entry: AuditHistoryEntry = {
    id: crypto.randomUUID(),
    savedAt: new Date().toISOString(),
    query,
    correlationId,
    result,
  };
  const updated = [entry, ...existing].slice(0, MAX_ENTRIES);
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  } catch {
    // localStorage quota exceeded — drop oldest and retry
    const trimmed = [entry, ...existing].slice(0, Math.max(1, MAX_ENTRIES - 2));
    localStorage.setItem(STORAGE_KEY, JSON.stringify(trimmed));
    return trimmed;
  }
  return updated;
}

export function deleteAuditEntry(id: string): AuditHistoryEntry[] {
  const updated = loadAuditHistory().filter((e) => e.id !== id);
  localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  return updated;
}

export function clearAuditHistory(): void {
  localStorage.removeItem(STORAGE_KEY);
}
