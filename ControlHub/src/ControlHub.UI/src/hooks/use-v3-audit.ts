import { useState, useCallback, useEffect, useRef } from 'react';
import { investigateV3, getAgentTrace, V3InvestigateRequest, V3InvestigateResponse, AgentTraceResponse } from '@/services/api/audit';
import {
  AuditHistoryEntry,
  loadAuditHistory,
  saveAuditResult,
  deleteAuditEntry,
  clearAuditHistory,
} from '@/lib/audit-history';

export interface UseV3AuditReturn {
  result: V3InvestigateResponse | null;
  trace: AgentTraceResponse | null;
  isLoading: boolean;
  error: string | null;
  history: AuditHistoryEntry[];

  investigate: (request: V3InvestigateRequest) => Promise<void>;
  fetchTrace: () => Promise<void>;
  reset: () => void;
  restoreFromHistory: (entry: AuditHistoryEntry) => void;
  deleteHistoryEntry: (id: string) => void;
  clearHistory: () => void;
}

// Module-level store — survives component unmount/remount
const v3Store = {
  result: null as V3InvestigateResponse | null,
  trace: null as AgentTraceResponse | null,
  isLoading: false,
  error: null as string | null,
};

export function useV3Audit(): UseV3AuditReturn {
  const mountedRef = useRef(true);

  // Initialize from store (restores state on remount)
  const [result, setResult] = useState<V3InvestigateResponse | null>(v3Store.result);
  const [trace, setTrace] = useState<AgentTraceResponse | null>(v3Store.trace);
  const [isLoading, setIsLoading] = useState(v3Store.isLoading);
  const [error, setError] = useState<string | null>(v3Store.error);
  const [history, setHistory] = useState<AuditHistoryEntry[]>(() => loadAuditHistory());

  useEffect(() => {
    mountedRef.current = true;
    // Sync from store on remount (in case fetch completed while unmounted)
    setResult(v3Store.result);
    setTrace(v3Store.trace);
    setIsLoading(v3Store.isLoading);
    setError(v3Store.error);
    setHistory(loadAuditHistory());

    return () => { mountedRef.current = false; };
  }, []);

  const investigate = useCallback(async (request: V3InvestigateRequest) => {
    setIsLoading(true);
    v3Store.isLoading = true;
    setError(null);
    v3Store.error = null;
    try {
      const response = await investigateV3(request);
      v3Store.result = response;
      v3Store.isLoading = false;
      const updated = saveAuditResult(request.query, request.correlationId, response);
      if (mountedRef.current) {
        setResult(response);
        setIsLoading(false);
        setHistory(updated);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Investigation failed';
      v3Store.error = message;
      v3Store.isLoading = false;
      if (mountedRef.current) {
        setError(message);
        setIsLoading(false);
      }
    }
  }, []);

  const fetchTrace = useCallback(async () => {
    try {
      const response = await getAgentTrace();
      v3Store.trace = response;
      if (mountedRef.current) {
        setTrace(response);
      }
    } catch (err) {
      console.error('Failed to fetch trace:', err);
    }
  }, []);

  const reset = useCallback(() => {
    v3Store.result = null;
    v3Store.trace = null;
    v3Store.error = null;
    setResult(null);
    setTrace(null);
    setError(null);
  }, []);

  const restoreFromHistory = useCallback((entry: AuditHistoryEntry) => {
    v3Store.result = entry.result;
    v3Store.trace = null;
    v3Store.error = null;
    setResult(entry.result);
    setTrace(null);
    setError(null);
  }, []);

  const deleteHistoryEntry = useCallback((id: string) => {
    setHistory(deleteAuditEntry(id));
  }, []);

  const clearHistory = useCallback(() => {
    clearAuditHistory();
    setHistory([]);
  }, []);

  return {
    result,
    trace,
    isLoading,
    error,
    history,
    investigate,
    fetchTrace,
    reset,
    restoreFromHistory,
    deleteHistoryEntry,
    clearHistory,
  };
}
