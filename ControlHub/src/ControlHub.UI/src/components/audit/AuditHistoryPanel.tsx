import { useState } from 'react';
import { Trash2, Clock, ChevronDown, ChevronRight, History, RotateCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { AuditHistoryEntry } from '@/lib/audit-history';

interface AuditHistoryPanelProps {
  history: AuditHistoryEntry[];
  onRestore: (entry: AuditHistoryEntry) => void;
  onDelete: (id: string) => void;
  onClearAll: () => void;
}

function formatRelativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

export function AuditHistoryPanel({ history, onRestore, onDelete, onClearAll }: AuditHistoryPanelProps) {
  const [isOpen, setIsOpen] = useState(false);

  if (history.length === 0) return null;

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <Card>
        <CardHeader className="py-3 px-4">
          <CollapsibleTrigger className="flex w-full items-center justify-between">
            <CardTitle className="flex items-center gap-2 text-sm font-medium">
              <History className="h-4 w-4" />
              Investigation History
              <Badge variant="secondary" className="ml-1 text-xs">{history.length}/10</Badge>
            </CardTitle>
            <div className="flex items-center gap-2">
              {isOpen && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs text-muted-foreground hover:text-destructive"
                  onClick={(e) => { e.stopPropagation(); onClearAll(); }}
                >
                  Clear all
                </Button>
              )}
              {isOpen ? <ChevronDown className="h-4 w-4 text-muted-foreground" /> : <ChevronRight className="h-4 w-4 text-muted-foreground" />}
            </div>
          </CollapsibleTrigger>
        </CardHeader>
        <CollapsibleContent>
          <CardContent className="px-4 pb-4 pt-0 space-y-2">
            {history.map((entry) => (
              <HistoryEntryRow
                key={entry.id}
                entry={entry}
                onRestore={onRestore}
                onDelete={onDelete}
              />
            ))}
          </CardContent>
        </CollapsibleContent>
      </Card>
    </Collapsible>
  );
}

function HistoryEntryRow({
  entry,
  onRestore,
  onDelete,
}: {
  entry: AuditHistoryEntry;
  onRestore: (entry: AuditHistoryEntry) => void;
  onDelete: (id: string) => void;
}) {
  const confidence = typeof entry.result.confidence === 'number' ? entry.result.confidence : 0;
  const passed = entry.result.verificationPassed === true;

  return (
    <div className="flex items-start gap-3 rounded-lg border bg-muted/30 px-3 py-2 text-sm hover:bg-muted/50 transition-colors">
      {/* Status dot */}
      <span
        className={`mt-0.5 h-2 w-2 flex-shrink-0 rounded-full ${passed ? 'bg-green-500' : 'bg-red-400'}`}
        title={passed ? 'Verification passed' : 'Verification failed'}
      />

      {/* Content */}
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium leading-snug text-foreground" title={entry.query}>
          {entry.query}
        </p>
        <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
          <Clock className="h-3 w-3 flex-shrink-0" />
          <span>{formatRelativeTime(entry.savedAt)}</span>
          <span>·</span>
          <span>{(confidence * 100).toFixed(0)}% conf.</span>
          {entry.correlationId && (
            <>
              <span>·</span>
              <span className="truncate max-w-[100px]" title={entry.correlationId}>
                {entry.correlationId.slice(0, 8)}…
              </span>
            </>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="flex flex-shrink-0 items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          title="Restore this result"
          onClick={() => onRestore(entry)}
        >
          <RotateCcw className="h-3.5 w-3.5" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-muted-foreground hover:text-destructive"
          title="Delete"
          onClick={() => onDelete(entry.id)}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}

export default AuditHistoryPanel;
