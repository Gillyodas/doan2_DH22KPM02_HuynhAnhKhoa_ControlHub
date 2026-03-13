import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import ReactMarkdown from 'react-markdown';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Loader2, Brain, Sparkles, RefreshCw, Layers, ChevronDown, ChevronRight, CheckCircle2 } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import { fetchJson } from '@/services/api/client';
import { loadAuth } from '@/auth/storage';
import { Badge } from '@/components/ui/badge';
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from '@/components/ui/collapsible';
import RunbookIngestDialog from '@/components/audit/RunbookIngestDialog';
import V3InvestigationPanel from '@/components/audit/V3InvestigationPanel';

interface LogTemplate {
    templateId: string;
    pattern: string;
    count: number;
    severity: string;
    firstSeen: string;
    lastSeen: string;
}

interface AnalysisResult {
    analysis: string;
    version?: string;
    logCount?: number;
    templates?: LogTemplate[];
    toolsUsed?: string[];
}

const AiAuditPage = () => {
    const { t, i18n } = useTranslation();
    const [searchParams] = useSearchParams();
    const [correlationId, setCorrelationId] = useState(searchParams.get('correlationId') || '');

    // Result State
    const [analysisResult, setAnalysisResult] = useState<AnalysisResult | null>(null);
    const [aiVersion, setAiVersion] = useState<string>('Loading...');

    // Loading State
    const [loading, setLoading] = useState(false);
    const [knowledgeLoading, setKnowledgeLoading] = useState(false);
    const [loadingStep, setLoadingStep] = useState<string>('');

    // Chat State
    const [activeTab, setActiveTab] = useState<'analyze' | 'chat' | 'v3'>('v3');
    const [chatQuestion, setChatQuestion] = useState('');
    const [chatAnswer, setChatAnswer] = useState<string | null>(null);
    const [chatLoading, setChatLoading] = useState(false);

    // Initial Load
    useEffect(() => {
        loadVersion();
        const idFromUrl = searchParams.get('correlationId');
        if (idFromUrl) {
            setCorrelationId(idFromUrl);
            analyzeLog(idFromUrl);
        }
    }, [searchParams]);

    const loadVersion = async () => {
        try {
            const data = await fetchJson<{ version: string; features: string[] }>('/api/audit/version');
            setAiVersion(data.version);
        } catch {
            setAiVersion('Offline');
        }
    };

    const analyzeLog = async (id: string) => {
        if (!id) return;
        setLoading(true);
        setAnalysisResult(null);
        setLoadingStep('Initializing Agent...');

        try {
            const lang = i18n.language || 'en';
            const authData = loadAuth();
            const accessToken = authData?.accessToken || '';

            // Simulate steps for UI feedback if V2.5
            if (aiVersion === 'V2.5') {
                setTimeout(() => setLoadingStep('Parsing Logs (Drain3)...'), 500);
                setTimeout(() => setLoadingStep('Sampling (Weighted Reservoir)...'), 1500);
                setTimeout(() => setLoadingStep('Investigating (Agentic Workflow)...'), 2500);
            } else {
                setLoadingStep('Reading logs...');
            }

            const data = await fetchJson<AnalysisResult>(`/api/audit/analyze/${id}?lang=${lang}`, {
                accessToken
            });
            setAnalysisResult(data);
        } catch (error) {
            console.error('AI Analysis Error:', error);
            setAnalysisResult({ analysis: 'Error: Session not found or API error.' });
        } finally {
            setLoading(false);
            setLoadingStep('');
        }
    };

    const handleAnalyze = () => analyzeLog(correlationId);

    const handleLearn = async () => {
        setKnowledgeLoading(true);
        try {
            await fetch('/api/audit/learn', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${loadAuth()?.accessToken}` }
            });
            alert(t('ai.knowledgeRefreshed', 'Knowledge Base updated!'));
        } catch (error) {
            alert('Failed to update knowledge base.');
        } finally {
            setKnowledgeLoading(false);
        }
    };

    const handleChat = async () => {
        if (!chatQuestion.trim()) return;
        setChatLoading(true);
        setChatAnswer(null);
        try {
            const lang = i18n.language || 'en';
            const accessToken = loadAuth()?.accessToken || '';
            const body = {
                question: chatQuestion,
                startTime: null,
                endTime: null
            };

            const data = await fetchJson<{ answer: string }>(`/api/audit/chat?lang=${lang}`, {
                method: 'POST',
                body,
                accessToken
            });
            setChatAnswer(data.answer);
        } catch (error) {
            console.error('AI Chat Error:', error);
            setChatAnswer('Error: Failed to get response from AI.');
        } finally {
            setChatLoading(false);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div className="flex items-center gap-3">
                    <div>
                        <h2 className="text-3xl font-bold tracking-tight">{t('ai.auditTitle', 'AI Log Audit')}</h2>
                        <p className="text-muted-foreground">
                            {t('ai.auditSubtitle', 'Agentic investigation enabled.')}
                        </p>
                    </div>
                    <Badge variant={aiVersion === 'V2.5' ? 'default' : 'secondary'} className="h-6">
                        {aiVersion}
                    </Badge>
                </div>
                <div className="flex gap-2">
                    <RunbookIngestDialog onSuccess={() => setChatAnswer('Runbook ingested! You can now ask about it.')} />
                    <Button variant="outline" onClick={handleLearn} disabled={knowledgeLoading}>
                        {knowledgeLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
                        {t('ai.refreshKnowledge', 'Refresh Knowledge')}
                    </Button>
                </div>
            </div>

            {/* Custom Tabs */}
            <div className="flex space-x-1 rounded-xl bg-muted p-1 w-fit">
                <button
                    onClick={() => setActiveTab('v3')}
                    className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${activeTab === 'v3' ? 'bg-background text-foreground shadow' : 'text-muted-foreground hover:bg-background/50'}`}
                >
                    🤖 V3.0 Agent
                </button>
                <button
                    onClick={() => setActiveTab('analyze')}
                    className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${activeTab === 'analyze' ? 'bg-background text-foreground shadow' : 'text-muted-foreground hover:bg-background/50'}`}
                >
                    Investigation
                </button>
                <button
                    onClick={() => setActiveTab('chat')}
                    className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${activeTab === 'chat' ? 'bg-background text-foreground shadow' : 'text-muted-foreground hover:bg-background/50'}`}
                >
                    Chat with Logs
                </button>
            </div>

            {activeTab === 'v3' ? (
                <V3InvestigationPanel />
            ) : activeTab === 'analyze' ? (
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                    {/* Input Section */}
                    <Card className="col-span-1 h-fit">
                        <CardHeader>
                            <CardTitle>{t('ai.inputSession', 'Session Analysis')}</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex flex-col space-y-2">
                                <label className="text-sm font-medium">Correlation ID</label>
                                <Input
                                    placeholder="e.g. 550e8400-e29b..."
                                    value={correlationId}
                                    onChange={(e) => setCorrelationId(e.target.value)}
                                />
                            </div>
                            <Button className="w-full" onClick={handleAnalyze} disabled={loading || !correlationId}>
                                {loading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Sparkles className="mr-2 h-4 w-4" />}
                                {loading ? loadingStep : t('ai.analyzeBtn', 'Investigate')}
                            </Button>

                            {/* Tool Usage Indicator */}
                            {analysisResult?.toolsUsed && (
                                <div className="mt-4 border-t pt-2">
                                    <h4 className="text-xs font-semibold uppercase text-muted-foreground mb-2">Agent Execution Trace</h4>
                                    <ul className="space-y-1">
                                        {analysisResult.toolsUsed.map((tool, i) => (
                                            <li key={i} className="text-xs flex items-center gap-2 text-green-600">
                                                <CheckCircle2 className="h-3 w-3" />
                                                {tool}
                                            </li>
                                        ))}
                                    </ul>
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    {/* Result Section */}
                    <div className="col-span-1 md:col-span-2 lg:col-span-2 space-y-4">
                        <Card className="min-h-[400px]">
                            <CardHeader>
                                <CardTitle className="flex items-center gap-2">
                                    <Brain className="h-5 w-5 text-primary" />
                                    {t('ai.resultTitle', 'Agent Findings')}
                                </CardTitle>
                            </CardHeader>
                            <CardContent className="prose prose-sm dark:prose-invert max-w-none">
                                {loading && (
                                    <div className="flex flex-col items-center justify-center h-64 space-y-4 text-muted-foreground">
                                        <Loader2 className="h-8 w-8 animate-spin" />
                                        <p>{loadingStep || t('ai.analyzing', 'Sifting through logs...')}</p>
                                    </div>
                                )}
                                {!loading && analysisResult && (
                                    <ReactMarkdown>{analysisResult.analysis ?? ''}</ReactMarkdown>
                                )}
                                {!loading && !analysisResult && (
                                    <div className="flex flex-col items-center justify-center h-64 text-muted-foreground border-2 border-dashed rounded-lg">
                                        <Sparkles className="h-8 w-8 mb-2 opacity-50" />
                                        <p>{t('ai.placeholder', 'Enter a Correlation ID to start analysis')}</p>
                                    </div>
                                )}
                            </CardContent>
                        </Card>

                        {/* Collapsible Templates Section (V2.5 only) */}
                        {analysisResult?.templates && analysisResult.templates.length > 0 && (
                            <CollapsibleCard title="Log Templates (Drain3 Extracted)">
                                <div className="space-y-2 max-h-[300px] overflow-y-auto">
                                    {analysisResult.templates.map((tmpl) => (
                                        <div key={tmpl.templateId} className="flex gap-3 text-xs border p-2 rounded bg-muted/30">
                                            <Badge variant={tmpl.severity === 'Error' || tmpl.severity === 'Fatal' ? 'destructive' : 'outline'}>
                                                {tmpl.severity}
                                            </Badge>
                                            <Badge variant="secondary">x{tmpl.count}</Badge>
                                            <code className="flex-1 break-all">{tmpl.pattern}</code>
                                        </div>
                                    ))}
                                </div>
                            </CollapsibleCard>
                        )}
                    </div>
                </div>
            ) : (
                /* Chat Tab Logic (Same as before but simplified) */
                <div className="grid grid-cols-1">
                    <Card className="min-h-[600px] flex flex-col">
                        <CardHeader><CardTitle>Chat with Logs</CardTitle></CardHeader>
                        <CardContent className="flex-1 flex flex-col gap-4">
                            <div className="flex-1 overflow-y-auto border rounded-lg p-4 bg-muted/20 min-h-[400px]">
                                {chatLoading ? (
                                    <Loader2 className="h-8 w-8 animate-spin m-auto" />
                                ) : chatAnswer ? (
                                    <div className="prose prose-sm dark:prose-invert">
                                        <ReactMarkdown>{chatAnswer ?? ''}</ReactMarkdown>
                                    </div>
                                ) : (
                                    <p className="text-center text-muted-foreground mt-40">Ask about recent system activity...</p>
                                )}
                            </div>
                            <div className="flex gap-2">
                                <Input value={chatQuestion} onChange={(e) => setChatQuestion(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && handleChat()} placeholder="Ask specific questions..." />
                                <Button onClick={handleChat} disabled={chatLoading}>Send</Button>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            )}
        </div>
    );
};

// Helper Component
const CollapsibleCard = ({ title, children }: { title: string, children: React.ReactNode }) => {
    const [isOpen, setIsOpen] = useState(false);
    return (
        <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full border rounded-lg bg-card text-card-foreground shadow-sm">
            <div className="p-4 flex items-center justify-between cursor-pointer" onClick={() => setIsOpen(!isOpen)}>
                <div className="flex items-center gap-2 font-semibold">
                    <Layers className="h-4 w-4" />
                    {title}
                </div>
                <CollapsibleTrigger asChild>
                    <Button variant="ghost" size="sm" className="w-9 p-0">
                        {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                    </Button>
                </CollapsibleTrigger>
            </div>
            <CollapsibleContent className="px-4 pb-4">
                {children}
            </CollapsibleContent>
        </Collapsible>
    );
};

export default AiAuditPage;
