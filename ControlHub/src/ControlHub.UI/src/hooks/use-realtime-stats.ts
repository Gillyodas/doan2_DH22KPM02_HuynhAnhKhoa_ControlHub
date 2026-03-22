import { useState, useEffect, useCallback, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuth } from '@/auth/use-auth'

export interface LoginAttempt {
    timestamp: string
    isSuccess: boolean
    identifierType: string
    maskedIdentifier: string
    failureReason: string | null
}

export interface DashboardStats {
    totalRoles: number
    totalIdentifiers: number
    activeUsers: number
}

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'error'

export function useRealtimeStats() {
    const [activeUsers, setActiveUsers] = useState(0)
    const [totalRoles, setTotalRoles] = useState(0)
    const [totalIdentifiers, setTotalIdentifiers] = useState(0)
    const [loginAttempts, setLoginAttempts] = useState<LoginAttempt[]>([])
    const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('connecting')

    const connectionRef = useRef<signalR.HubConnection | null>(null)
    const { auth } = useAuth()

    const connect = useCallback(async () => {
        if (!auth?.accessToken || connectionRef.current?.state === signalR.HubConnectionState.Connected) {
            return
        }

        if (connectionRef.current) {
            try {
                await connectionRef.current.stop();
            } catch (e) {
                console.error("Error stopping previous connection", e);
            }
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${import.meta.env.VITE_API_URL || 'https://localhost:7110'}/hubs/dashboard`, {
                accessTokenFactory: () => auth.accessToken!,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Warning)
            .build()

        connection.on('ActiveUsersUpdated', (count: number) => {
            setActiveUsers(count)
        })

        connection.on('DashboardStatsUpdated', (stats: DashboardStats) => {
            setTotalRoles(stats.totalRoles)
            setTotalIdentifiers(stats.totalIdentifiers)
            setActiveUsers(stats.activeUsers)
        })

        connection.on('LoginAttemptsBatch', (batch: LoginAttempt[]) => {
            setLoginAttempts(prev => {
                const updated = [...batch, ...prev]
                return updated.slice(0, 100)
            })
        })

        connection.onreconnecting(() => setConnectionStatus('connecting'))
        connection.onreconnected(() => setConnectionStatus('connected'))
        connection.onclose(() => setConnectionStatus('disconnected'))

        try {
            await connection.start()
            connectionRef.current = connection
            setConnectionStatus('connected')

            await connection.invoke('RequestCurrentStats')
        } catch (error) {
            console.error('SignalR connection failed:', error)
            setConnectionStatus('error')
        }
    }, [auth?.accessToken])

    useEffect(() => {
        connect();

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        }
    }, [connect])

    return { activeUsers, totalRoles, totalIdentifiers, loginAttempts, connectionStatus }
}
